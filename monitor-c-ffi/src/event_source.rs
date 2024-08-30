use std::{
    collections::{HashMap, HashSet},
    convert::Infallible,
};

use itertools::Itertools;
use nom::{
    number::streaming::{be_f64, be_u16, be_u32, be_u64, be_u8},
    IResult, Parser,
};
use rtlola_interpreter::{input::EventFactory, rtlola_mir::InputReference};

use crate::error::{EventSourceError, InvalidAst, JsonError, ParseError, TypeError, Unreachable};

#[derive(Debug, Clone, Copy, PartialEq, Eq, PartialOrd, Ord)]
pub enum Type {
    UInt8,
    UInt16,
    UInt32,
    UInt64,
    Float64,
}

impl TryFrom<&str> for Type {
    type Error = TypeError;

    fn try_from(value: &str) -> Result<Self, Self::Error> {
        match value {
            "UInt8" => Ok(Type::UInt8),
            "UInt16" => Ok(Type::UInt16),
            "UInt32" => Ok(Type::UInt32),
            "UInt64" => Ok(Type::UInt64),
            "Float64" => Ok(Type::Float64),
            e => Err(TypeError(e.to_string())),
        }
    }
}

#[derive(Debug, Clone, PartialEq, Eq)]
pub enum ParserAST<T, R> {
    Seq(Vec<ParserAST<T, R>>),
    Split {
        switch: R,
        ty: HashMap<u64, ParserAST<T, R>>,
    },
    Element {
        stream: T,
        ty: Type,
    },
}

impl<'a> From<ParserAST<&'a str, &'a str>> for ParserAST<String, String> {
    fn from(value: ParserAST<&'a str, &'a str>) -> Self {
        match value {
            ParserAST::Seq(seq) => ParserAST::Seq(seq.into_iter().map(|ast| ast.into()).collect()),
            ParserAST::Split { switch, ty } => ParserAST::Split {
                switch: switch.to_string(),
                ty: ty
                    .into_iter()
                    .map(|(key, value)| (key, value.into()))
                    .collect(),
            },
            ParserAST::Element { stream, ty } => ParserAST::Element {
                stream: stream.to_string(),
                ty,
            },
        }
    }
}

#[derive(Debug, Clone, Copy)]
pub(crate) struct AstKeys {
    switch: Option<usize>,
    in_ref: Option<InputReference>,
}

#[derive(Debug, Clone, Copy)]
pub enum Value {
    UInt(u64),
    Float(f64),
}

impl TryFrom<Value> for u64 {
    type Error = TypeError;

    fn try_from(value: Value) -> Result<Self, Self::Error> {
        match value {
            Value::UInt(u) => Ok(u),
            Value::Float(_f) => Err(TypeError("Float64".to_string())),
        }
    }
}

impl Value {
    fn from_ty<'a>(ty: &Type, bytes: &'a [u8]) -> IResult<&'a [u8], Value> {
        match ty {
            Type::UInt8 => be_u8.map(|v| Value::UInt(v.into())).parse(bytes),
            Type::UInt16 => be_u16.map(|v| Value::UInt(v.into())).parse(bytes),
            Type::UInt32 => be_u32.map(|v| Value::UInt(v.into())).parse(bytes),
            Type::UInt64 => be_u64.map(Value::UInt).parse(bytes),
            Type::Float64 => be_f64.map(Value::Float).parse(bytes),
        }
    }
}

impl From<Value> for rtlola_interpreter::Value {
    fn from(value: Value) -> Self {
        match value {
            Value::UInt(u) => rtlola_interpreter::Value::Unsigned(u),
            Value::Float(f) => rtlola_interpreter::Value::Float(f.try_into().unwrap()),
        }
    }
}

impl ParserAST<String, String> {
    pub(crate) fn from_json(value: serde_json::Value) -> Result<Self, JsonError> {
        let seq = value
            .as_object()
            .map(|ordered_key_value| {
                ordered_key_value
                    .iter()
                    .map(|(key, value)| Self::parse(key, value))
                    .collect::<Result<Vec<_>, _>>()
            })
            .ok_or(JsonError::InvalidStart)??;
        Ok(ParserAST::Seq(seq))
    }

    fn parse(key: &str, value: &serde_json::Value) -> Result<Self, JsonError> {
        match value {
            serde_json::Value::String(obj) => Ok(ParserAST::Element {
                stream: key.to_string(),
                ty: obj.as_str().try_into().map_err(JsonError::InvalidType)?,
            }),
            serde_json::Value::Object(obj) => {
                if key == "payload" {
                    let payload_id = obj
                        .get("payload_id")
                        .and_then(|v| v.as_str())
                        .ok_or(JsonError::InvalidPayload)?;
                    let mut ty_map: HashMap<u64, ParserAST<String, String>> = HashMap::new();
                    if let Some(map) = value.as_object() {
                        for (payload_key, payload_value) in map {
                            if let Ok(id) = payload_key.parse::<u64>() {
                                ty_map.insert(id, Self::parse_payload(payload_value)?);
                            }
                        }
                    }
                    Ok(ParserAST::Split {
                        switch: payload_id.to_string(),
                        ty: ty_map,
                    })
                } else {
                    Err(JsonError::InvalidPayload)
                }
            }
            _ => Err(JsonError::InvalidJsonValue),
        }
    }

    fn parse_payload(value: &serde_json::Value) -> Result<ParserAST<String, String>, JsonError> {
        if let Some(payload) = value
            .as_object()
            .and_then(|v| v.get("data").and_then(|d| d.as_object()))
        {
            let seq = payload
                .iter()
                .map(|(key, value)| Self::parse(key, value))
                .collect::<Result<_, _>>()?;
            Ok(ParserAST::Seq(seq))
        } else {
            Err(JsonError::InvalidPayload)
        }
    }
}

impl ParserAST<String, String> {
    fn collect_splits(&self, currents: &mut Vec<String>) -> Result<Vec<String>, InvalidAst> {
        match self {
            ParserAST::Seq(seq) => seq
                .iter()
                .map(|cur| cur.collect_splits(currents))
                .flatten_ok()
                .collect(),
            ParserAST::Split { switch, ty: _ } => {
                if currents.contains(switch) {
                    Ok(vec![switch.clone()])
                } else {
                    Err(InvalidAst::SplitKeyNotFound(switch.to_string()))
                }
            }
            ParserAST::Element { stream, ty: _ } => {
                currents.push(stream.clone());
                Ok(Vec::new())
            }
        }
    }

    fn collect_input_streams(&self, input_streams: &mut HashSet<String>) {
        match self {
            ParserAST::Seq(seq) => seq
                .iter()
                .for_each(|ast| ast.collect_input_streams(input_streams)),
            ParserAST::Split { switch: _, ty } => ty
                .values()
                .for_each(|ast| ast.collect_input_streams(input_streams)),
            ParserAST::Element { stream, ty: _ } => {
                input_streams.insert(stream.to_string());
            }
        }
    }
}

impl ParserAST<AstKeys, usize> {
    pub(crate) fn new(
        ast: ParserAST<String, String>,
        map: &HashMap<String, InputReference>,
    ) -> Result<Self, InvalidAst> {
        let splits = ast
            .collect_splits(Vec::new().as_mut())?
            .into_iter()
            .enumerate()
            .collect();
        Self::new_iter(ast, map, &splits)
    }

    fn new_iter(
        ast: ParserAST<String, String>,
        map: &HashMap<String, InputReference>,
        splits: &Vec<(usize, String)>,
    ) -> Result<Self, InvalidAst> {
        match ast {
            ParserAST::Seq(seq) => {
                let seq = seq
                    .into_iter()
                    .map(|ast| ParserAST::new_iter(ast, map, splits))
                    .collect::<Result<_, _>>()?;
                Ok(Self::Seq(seq))
            }
            ParserAST::Split { switch, ty } => {
                let switch = splits
                    .iter()
                    .find_map(|(id, name)| (&switch == name).then_some(*id))
                    .ok_or(InvalidAst::SplitKeyNotFound(switch.to_string()))?;
                let ty = ty
                    .into_iter()
                    .map(|(key, ast)| ParserAST::new_iter(ast, map, splits).map(|ast| (key, ast)))
                    .collect::<Result<_, _>>()?;
                Ok(ParserAST::Split { switch, ty })
            }
            ParserAST::Element { stream, ty } => {
                let switch = splits
                    .iter()
                    .find_map(|(id, name)| (&stream == name).then_some(*id));
                let in_ref = map.get(&stream).copied();
                let stream = AstKeys { switch, in_ref };
                Ok(ParserAST::Element { stream, ty })
            }
        }
    }

    fn parse<'a>(
        &self,
        bytes: &'a [u8],
        input_values: &mut HashMap<InputReference, Value>,
        split_values: &mut HashMap<usize, u64>,
    ) -> IResult<&'a [u8], (), ParseError> {
        match self {
            ParserAST::Seq(seq) => {
                let mut bytes = bytes;
                for element in seq {
                    bytes = element.parse(bytes, input_values, split_values)?.0;
                }
                Ok((bytes, ()))
            }
            ParserAST::Split {
                switch: switch_counter,
                ty,
            } => {
                let switch = split_values.get(switch_counter).ok_or(nom::Err::Failure(
                    ParseError::Unreachable(Unreachable::from("Key should be avaible")),
                ))?;
                let ast = ty
                    .get(switch)
                    .ok_or(nom::Err::Failure(ParseError::InvalidSplitKey {
                        switch_counter: *switch_counter,
                        value: *switch,
                    }))?;
                ast.parse(bytes, input_values, split_values)
            }
            ParserAST::Element { stream, ty } => {
                let (rest, value) = Value::from_ty(ty, bytes).map_err(|e| {
                    e.map(|e| {
                        ParseError::ValueConversion(nom::error::Error::new(
                            e.input.to_vec(),
                            e.code,
                        ))
                    })
                })?;
                let AstKeys { switch, in_ref } = stream;
                if let Some(in_ref) = in_ref {
                    let res = input_values.insert(*in_ref, value);
                    assert!(res.is_none());
                }
                if let Some(switch) = switch {
                    let key: u64 = value.try_into().unwrap();
                    split_values.insert(*switch, key);
                }
                match in_ref {
                    Some(in_ref) => {
                        input_values.insert(*in_ref, value);
                    }
                    None => {}
                };
                Ok((rest, ()))
            }
        }
    }
}

pub struct InputStreamMap {
    pub(crate) len: usize,
}

impl rtlola_interpreter::input::EventFactory for InputStreamMap {
    type Record = HashMap<InputReference, Value>;

    type Error = Infallible;

    type CreationData = HashSet<String>;

    fn try_new(
        map: HashMap<String, InputReference>,
        setup_data: Self::CreationData,
    ) -> Result<(Self, Vec<String>), rtlola_interpreter::input::EventFactoryError> {
        Ok((
            InputStreamMap { len: map.len() },
            setup_data.into_iter().collect(),
        ))
    }

    fn get_event(
        &self,
        rec: Self::Record,
    ) -> Result<rtlola_interpreter::monitor::Event, rtlola_interpreter::input::EventFactoryError>
    {
        let mut res: Vec<_> = (0..self.len)
            .map(|_| rtlola_interpreter::Value::None)
            .collect();
        rec.into_iter().for_each(|(sr, v)| {
            res[sr] = v.into();
        });
        Ok(res)
    }
}

pub struct EventSource {
    pub(crate) ast: ParserAST<AstKeys, usize>,
    pub(crate) factory: InputStreamMap,
}

impl EventSource {
    pub(crate) fn new(
        ast: ParserAST<String, String>,
        map: HashMap<String, InputReference>,
    ) -> Result<Self, EventSourceError> {
        let mut setup_data = HashSet::new();
        ast.collect_input_streams(&mut setup_data);
        let ast = ParserAST::<AstKeys, usize>::new(ast, &map).map_err(EventSourceError::Ast)?;

        let factory = InputStreamMap::new(map, setup_data).map_err(EventSourceError::Factory)?;

        Ok(Self { ast, factory })
    }

    pub(crate) fn get_event(
        &self,
        rec: &[u8],
    ) -> Result<(rtlola_interpreter::monitor::Event, usize), EventSourceError> {
        let mut input_values = HashMap::new();
        let mut split_values = HashMap::new();
        let (rest_byte, _) = self
            .ast
            .parse(rec, &mut input_values, &mut split_values)
            .map_err(|e| match e {
                nom::Err::Incomplete(_e) => EventSourceError::Incomplete,
                nom::Err::Error(e) | nom::Err::Failure(e) => {
                    EventSourceError::Ast(InvalidAst::ParseError(e))
                }
            })?;
        let parsed_bytes = rec.len() - rest_byte.len();
        let event = self
            .factory
            .get_event(input_values)
            .map_err(EventSourceError::Factory)?;
        Ok((event, parsed_bytes))
    }
}

#[cfg(test)]
mod tests {
    use rtlola_interpreter::Value;

    use crate::error::EventSourceError;

    use super::{EventSource, ParserAST};

    #[test]
    fn single_element() {
        let ast = ParserAST::<String, String>::Element {
            stream: "msg_id".to_string(),
            ty: super::Type::UInt8,
        };
        let map = vec![("msg_id".to_string(), 0)].into_iter().collect();
        let source = EventSource::new(ast, map).unwrap();
        let bytes = vec![4_u8, 5_u8];
        let (event, num_bytes) = source.get_event(&bytes).unwrap();
        assert_eq!(num_bytes, 1);
        assert_eq!(event, vec![Value::Unsigned(4)]);
        let bytes = vec![];
        let res = source.get_event(&bytes);
        assert!(matches!(res, Err(EventSourceError::Incomplete)));
    }
    #[test]
    fn sequence() {
        let ast = ParserAST::Seq(vec![
            ParserAST::<String, String>::Element {
                stream: "msg_id".to_string(),
                ty: super::Type::UInt8,
            },
            ParserAST::<String, String>::Element {
                stream: "node_id".to_string(),
                ty: super::Type::UInt16,
            },
        ]);
        let map = vec![("msg_id".to_string(), 0), ("node_id".to_string(), 1)]
            .into_iter()
            .collect();
        let source = EventSource::new(ast, map).unwrap();
        let bytes = vec![4_u8, 0_u8, 5_u8];
        let (event, num_bytes) = source.get_event(&bytes).unwrap();
        assert_eq!(num_bytes, 3);
        assert_eq!(event, vec![Value::Unsigned(4), Value::Unsigned(5)]);
        let bytes = vec![];
        let res = source.get_event(&bytes);
        assert!(matches!(res, Err(EventSourceError::Incomplete)));
    }
    #[test]
    fn split() {
        let ast = ParserAST::Seq(vec![
            ParserAST::<String, String>::Element {
                stream: "msg_id".to_string(),
                ty: super::Type::UInt8,
            },
            ParserAST::<String, String>::Element {
                stream: "node_id".to_string(),
                ty: super::Type::UInt16,
            },
            ParserAST::Split {
                switch: "msg_id".to_string(),
                ty: vec![
                    (
                        0,
                        ParserAST::<String, String>::Element {
                            stream: "acc".to_string(),
                            ty: super::Type::UInt8,
                        },
                    ),
                    (
                        1,
                        ParserAST::<String, String>::Element {
                            stream: "velo".to_string(),
                            ty: super::Type::UInt16,
                        },
                    ),
                ]
                .into_iter()
                .collect(),
            },
        ]);
        let map = vec![
            ("msg_id".to_string(), 0),
            ("node_id".to_string(), 1),
            ("velo".to_string(), 2),
        ]
        .into_iter()
        .collect();
        let source = EventSource::new(ast, map).unwrap();
        let bytes = vec![1_u8, 0_u8, 5_u8, 0_u8, 7_u8];
        let (event, num_bytes) = source.get_event(&bytes).unwrap();
        assert_eq!(num_bytes, 5);
        assert_eq!(
            event,
            vec![Value::Unsigned(1), Value::Unsigned(5), Value::Unsigned(7)]
        );
        let bytes = vec![0_u8, 0_u8, 5_u8, 0_u8, 7_u8];
        let (event, num_bytes) = source.get_event(&bytes).unwrap();
        assert_eq!(num_bytes, 4);
        assert_eq!(
            event,
            vec![Value::Unsigned(0), Value::Unsigned(5), Value::None]
        );
    }
}

#[cfg(test)]
mod json_parser_test {
    use serde_json::Value;

    use crate::event_source::ParserAST;
    use crate::event_source::Type::{Float64, UInt16, UInt8};

    #[test]
    fn unsupported_json_value_num_fail() {
        let json_str = r#"
        {
            "name": "test_non_type"
        }
    "#;
        let value: Value = serde_json::from_str(json_str).unwrap();

        let ast = ParserAST::from_json(value);
        assert!(ast.is_err());
    }

    #[test]
    fn type_conversion_error() {
        // Should throw an error
        let json_str = r#"
        {
            "id": 30
        }
    "#;
        let value: Value = serde_json::from_str(json_str).unwrap();

        let ast = ParserAST::from_json(value);
        assert!(ast.is_err());
    }

    #[test]
    fn invalid_start_error() {
        let json_str = r#"
        [{
            "test": "Float64"
        }]
    "#;
        let value: Value = serde_json::from_str(json_str).unwrap();

        let ast = ParserAST::from_json(value);
        assert!(ast.is_err());
    }

    #[test]
    fn without_payload() {
        let json_str = r#"
        {
            "test": "Float64"
        }
    "#;
        let value: Value = serde_json::from_str(json_str).unwrap();

        let ast = ParserAST::from_json(value).unwrap();
        let result = ParserAST::Seq(
            vec![ParserAST::Element {
                stream: "test".to_string(),
                ty: Float64,
            }]
            .into_iter()
            .collect(),
        );
        assert_eq!(ast, result)
    }

    #[test]
    fn empty_json_object() {
        let json_str = r#"
        {}
    "#;
        let value: Value = serde_json::from_str(json_str).unwrap();

        let ast = ParserAST::from_json(value);
        assert!(ast.is_err());
    }

    #[test]
    fn basic_string_elements() {
        let json_str = r#"
        {
            "test": "Float64",
            "key": "UInt16"
        }
    "#;
        let value: Value = serde_json::from_str(json_str).unwrap();

        let ast = ParserAST::from_json(value).unwrap();
        let result = ParserAST::Seq(
            vec![
                ParserAST::Element {
                    stream: "test".to_string(),
                    ty: Float64,
                },
                ParserAST::Element {
                    stream: "key".to_string(),
                    ty: UInt16,
                },
            ]
            .into_iter()
            .collect(),
        );
        assert_eq!(ast, result)
    }

    #[test]
    fn empty_payload() {
        let json_str = r#"
        {
            "test": "Float64",
            "key": "UInt16",
            "payload": {

            }
        }
    "#;
        let value: Value = serde_json::from_str(json_str).unwrap();

        let ast = ParserAST::from_json(value);
        assert!(ast.is_err());
    }

    #[test]
    fn no_data() {
        let json_str = r#"
        {
            "test": "Float64",
            "key": "UInt16",
            "payload": {
                "switch": "test"
            }
        }
    "#;
        let value: Value = serde_json::from_str(json_str).unwrap();

        let ast = ParserAST::from_json(value);
        assert!(ast.is_err());
    }

    #[test]
    fn empty_data() {
        let json_str = r#"
        {
            "test": "Float64",
            "key": "UInt16",
            "payload": {
                "payload_id": "test",
                "data": {}
            }
        }
    "#;
        let value: Value = serde_json::from_str(json_str).unwrap();

        let ast = ParserAST::from_json(value);
        assert!(ast.is_err());
    }
    #[test]
    fn simple_data() {
        let json_str = r#"
        {
            "key": "UInt16",
            "payload": {
                "payload_id": "test",
                "1": {
                    "data": {
                        "key_data": "Float64"
                    }
                }
            }
        }
    "#;
        let value: Value = serde_json::from_str(json_str).unwrap();

        let ast = ParserAST::from_json(value).unwrap();
        let result = ParserAST::Seq(vec![
            ParserAST::Element {
                stream: "key".to_string(),
                ty: UInt16,
            },
            ParserAST::Split {
                switch: "test".to_string(),
                ty: vec![(
                    1,
                    ParserAST::Seq(vec![ParserAST::Element {
                        stream: "key_data".to_string(),
                        ty: Float64,
                    }]),
                )]
                .into_iter()
                .collect(),
            },
        ]);
        assert_eq!(ast, result)
    }

    #[test]
    fn array_type_error() {
        let json_str = r#"
        {
            "id": ["Float64"]
        }
    "#;
        let value: Value = serde_json::from_str(json_str).unwrap();

        let ast = ParserAST::from_json(value);
        assert!(ast.is_err());
    }

    #[test]
    fn simple_payload() {
        let json_str = r#"
        {
            "payload": {
                "payload_id": "msg_id",
                "1": {
                    "name": "GPS",
                    "description": "GPS data including latitude, longitude and altitude.",
                    "data": {
                        "lat": "Float64",
                        "lon": "Float64",
                        "alt": "Float64"
                    }
                }
            }
        }
    "#;
        let value: Value = serde_json::from_str(json_str).unwrap();

        let ast = ParserAST::from_json(value).unwrap();

        let result = ParserAST::Seq(vec![ParserAST::Split {
            switch: "msg_id".to_string(),
            ty: vec![(
                1,
                ParserAST::Seq(vec![
                    ParserAST::Element {
                        stream: "lat".to_string(),
                        ty: Float64,
                    },
                    ParserAST::Element {
                        stream: "lon".to_string(),
                        ty: Float64,
                    },
                    ParserAST::Element {
                        stream: "alt".to_string(),
                        ty: Float64,
                    },
                ]),
            )]
            .into_iter()
            .collect(),
        }]);
        assert_eq!(ast, result)
    }

    #[test]
    fn complete_example() {
        let json_str = r#"
        {
            "timestamp": "Float64",
            "msg_id": "UInt8",
            "node_id": "UInt16",
            "payload": {
                "payload_id": "msg_id",
                "1": {
                    "name": "GPS",
                    "description": "GPS data including latitude, longitude and altitude.",
                    "data": {
                        "lat": "Float64",
                        "lon": "Float64",
                        "alt": "Float64"
                    }
                },
                "2": {
                    "name": "Velocity",
                    "description": "Aircraft velocity vector in meters per second.",
                    "data": {
                        "x": "Float64",
                        "y": "Float64",
                        "z": "Float64"
                    }
                }
            },
            "checksum": "UInt8"
        }
    "#;
        let value: Value = serde_json::from_str(json_str).unwrap();

        let ast = ParserAST::from_json(value).unwrap();

        let result = ParserAST::Seq(vec![
            ParserAST::Element {
                stream: "timestamp".to_string(),
                ty: Float64,
            },
            ParserAST::Element {
                stream: "msg_id".to_string(),
                ty: UInt8,
            },
            ParserAST::Element {
                stream: "node_id".to_string(),
                ty: UInt16,
            },
            ParserAST::Split {
                switch: "msg_id".to_string(),
                ty: vec![
                    (
                        1,
                        ParserAST::Seq(vec![
                            ParserAST::Element {
                                stream: "lat".to_string(),
                                ty: Float64,
                            },
                            ParserAST::Element {
                                stream: "lon".to_string(),
                                ty: Float64,
                            },
                            ParserAST::Element {
                                stream: "alt".to_string(),
                                ty: Float64,
                            },
                        ]),
                    ),
                    (
                        2,
                        ParserAST::Seq(vec![
                            ParserAST::Element {
                                stream: "x".to_string(),
                                ty: Float64,
                            },
                            ParserAST::Element {
                                stream: "y".to_string(),
                                ty: Float64,
                            },
                            ParserAST::Element {
                                stream: "z".to_string(),
                                ty: Float64,
                            },
                        ]),
                    ),
                ]
                .into_iter()
                .collect(),
            },
            ParserAST::Element {
                stream: "checksum".to_string(),
                ty: UInt8,
            },
        ]);
        assert_eq!(ast, result)
    }
}
