use std::{convert::Infallible, error::Error, fmt::Debug, marker::PhantomData};

use rtlola_interpreter::{
    monitor::{TotalIncremental, VerdictRepresentation, Verdicts},
    rtlola_mir::{RtLolaMir, StreamReference, Type},
    time::{OutputTimeRepresentation, TimeRepresentation},
    Value,
};

use crate::{
    error::{StreamError, Unreachable},
    OutputTime,
};

pub trait VerdictsSink<V: VerdictRepresentation, T: OutputTimeRepresentation> {
    /// Error Type of an [VerdictsSink] implementation
    type Error: Error;
    /// Return Type of an [VerdictsSink] implementation
    type Return;

    /// Defines how the verdicts of the monitor needs to be handled
    fn sink(&mut self, verdicts: &Verdicts<V, T>) -> Result<Vec<Self::Return>, Self::Error> {
        let Verdicts { timed, event, ts } = verdicts;
        timed
            .iter()
            .map(|(ts, verdict)| (ts, verdict))
            .chain(vec![(ts, event)])
            .map(|(ts, verdict)| self.sink_verdict(ts, verdict))
            .collect::<Result<Vec<_>, _>>()
    }
    /// Defines how one verdict of the monitor needs to be handled, timed and event-based
    fn sink_verdict(
        &mut self,
        ts: &<T as TimeRepresentation>::InnerTime,
        verdict: &V,
    ) -> Result<Self::Return, Self::Error>;
}

pub trait VerdictFactory<V: VerdictRepresentation, T: OutputTimeRepresentation>
where
    Self: Sized + Debug,
{
    type Error: Error + 'static;
    type Config;
    type Rec;

    fn new(cfg: Self::Config) -> Result<Self, Self::Error>;
    fn create(
        &mut self,
        ts: &<T as TimeRepresentation>::InnerTime,
        verdict: &V,
    ) -> Result<Self::Rec, Self::Error>;
}

pub struct VectorVerdictsSink<
    V: VerdictRepresentation,
    T: OutputTimeRepresentation,
    F: VerdictFactory<V, T>,
> {
    factory: F,
    phantom: PhantomData<(V, T)>,
}

impl<V: VerdictRepresentation, T: OutputTimeRepresentation, F: VerdictFactory<V, T>>
    VectorVerdictsSink<V, T, F>
{
    pub fn new(cfg: F::Config) -> Result<Self, F::Error> {
        let factory = F::new(cfg)?;
        Ok(Self {
            factory,
            phantom: PhantomData,
        })
    }
}

impl<V: VerdictRepresentation, T: OutputTimeRepresentation, F: VerdictFactory<V, T>>
    VerdictsSink<V, T> for VectorVerdictsSink<V, T, F>
{
    type Error = F::Error;

    type Return = F::Rec;

    fn sink_verdict(
        &mut self,
        ts: &<T as TimeRepresentation>::InnerTime,
        verdict: &V,
    ) -> Result<Self::Return, Self::Error> {
        self.factory.create(ts, verdict)
    }
}

#[derive(Debug, Clone)]
pub struct GraphVerdictFactory {
    points: Vec<GraphPoint<StreamReference>>,
}

impl VerdictFactory<TotalIncremental, OutputTime> for GraphVerdictFactory {
    type Error = Infallible;

    type Config = Vec<GraphPoint<StreamReference>>;

    type Rec = Vec<GraphPoint<Value>>;

    fn new(cfg: Self::Config) -> Result<Self, Self::Error> {
        Ok(GraphVerdictFactory { points: cfg })
    }

    fn create(
        &mut self,
        _ts: &<OutputTime as TimeRepresentation>::InnerTime,
        verdict: &TotalIncremental,
    ) -> Result<Self::Rec, Self::Error> {
        Ok(self
            .points
            .iter()
            .flat_map(|point| GraphPoint::<Value>::new(point, verdict))
            .collect())
    }
}

#[derive(Debug, Clone, Copy)]
pub struct GraphPoint<T> {
    pub(crate) graph_id: u8,
    pub(crate) line_id: u8,
    pub(crate) point: T,
}

#[allow(unused)]
impl GraphPoint<String> {
    pub(crate) fn new(graph_id: u8, line_id: u8, point: String) -> Self {
        Self {
            graph_id,
            line_id,
            point,
        }
    }
}

// go from list of streams we want new point from to list with stream references, then get values based on the stream references
impl GraphPoint<StreamReference> {
    pub(crate) fn new(point: GraphPoint<String>, ir: &RtLolaMir) -> Result<Self, StreamError> {
        let GraphPoint {
            graph_id,
            line_id,
            point,
        } = point;
        let sr = ir
            // does this only contain the streams we want to visualize?
            .all_streams()
            .find(|sr| ir.stream(*sr).name() == point)
            .ok_or(StreamError::StreamNotFound(point.clone()))?;
        match ir.stream(sr).ty() {
            Type::Tuple(types) => match types[..] {
                [Type::Float(_), Type::Float(_), Type::Bool] => Ok(()),
                _ => Err(StreamError::InvalidType(point.clone())),
            },
            _ => Err(StreamError::InvalidType(point)),
        }?;
        Ok(Self {
            graph_id,
            line_id,
            point: sr,
        })
    }
}

impl GraphPoint<Value> {
    pub(crate) fn new(
        point: &GraphPoint<StreamReference>,
        verdict: &TotalIncremental,
    ) -> Option<Self> {
        let GraphPoint {
            graph_id,
            line_id,
            point,
        } = point;
        value(*point, verdict).map(|value| GraphPoint {
            graph_id: *graph_id,
            line_id: *line_id,
            point: value.clone(),
        })
    }
}

impl GraphPoint<(f64, f64, bool)> {
    pub(crate) fn new(point: GraphPoint<Value>) -> Result<Self, Unreachable> {
        let GraphPoint {
            graph_id,
            line_id,
            point,
        } = point;
        match point {
            Value::Tuple(point) => match point.iter().collect::<Vec<_>>().as_slice() {
                [Value::Float(x_coord), Value::Float(y_coord), Value::Bool(b)] => Ok(Self {
                    graph_id,
                    line_id,
                    point: ((*x_coord).into(), (*y_coord).into(), *b),
                }),
                _ => Err(Unreachable::from("Tuple is invalid")),
            },
            _ => Err(Unreachable::from("Point is not a tuple")),
        }
    }
}

// Move to interpreter
fn value(sr: StreamReference, verdict: &TotalIncremental) -> Option<&Value> {
    match sr {
        StreamReference::In(ir) => {
            verdict
                .inputs
                .iter()
                .find_map(|(idx, v)| if ir == *idx { Some(v) } else { None })
        }
        StreamReference::Out(or) => verdict
            .outputs
            .iter()
            .find_map(|(idx, v)| if or == *idx { Some(v) } else { None })
            .and_then(|changes| {
                changes.iter().find_map(|change| match change {
                    rtlola_interpreter::monitor::Change::Spawn(_)
                    | rtlola_interpreter::monitor::Change::Close(_) => None,
                    rtlola_interpreter::monitor::Change::Value(instance, v)
                        if instance.is_none() =>
                    {
                        Some(v)
                    }
                    rtlola_interpreter::monitor::Change::Value(instance, v)
                        if instance.as_ref() == Some(&Vec::new()) =>
                    {
                        Some(v)
                    }
                    rtlola_interpreter::monitor::Change::Value(_instance, _v) => unreachable!(),
                })
            }),
    }
}

#[derive(Debug, Clone, Copy)]
pub struct Limit<T> {
    pub(crate) graph_id: u8,
    pub(crate) is_x: bool,
    pub(crate) stream: T,
}

#[allow(unused)]
impl Limit<String> {
    pub(crate) fn new(graph_id: u8, is_x: bool, stream: String) -> Self {
        Self {
            graph_id,
            is_x,
            stream,
        }
    }
}

// go from list of streams we want new point from to list with stream references, then get values based on the stream references
impl Limit<StreamReference> {
    pub(crate) fn new(point: Limit<String>, ir: &RtLolaMir) -> Result<Self, StreamError> {
        let Limit {
            graph_id,
            is_x,
            stream,
        } = point;
        let sr = ir
            // does this only contain the streams we want to visualize?
            .all_streams()
            .find(|sr| {
                ir.stream(*sr).name() == stream
            })
            .ok_or(StreamError::StreamNotFound(stream.clone()))?;
        match ir.stream(sr).ty() {
            Type::Tuple(types) => match types[..] {
                [Type::Float(_), Type::Float(_)] => Ok(()),
                _ => Err(StreamError::InvalidType(stream.clone())),
            },
            _ => Err(StreamError::InvalidType(stream)),
        }?;
        Ok(Self {
            graph_id,
            is_x,
            stream: sr,
        })
    }
}

impl Limit<Value> {
    pub(crate) fn new(point: &Limit<StreamReference>, verdict: &TotalIncremental) -> Option<Self> {
        let Limit {
            graph_id,
            is_x,
            stream,
        } = point;
        value(*stream, verdict).map(|value| Limit {
            graph_id: *graph_id,
            is_x: *is_x,
            stream: value.clone(),
        })
    }
}

impl Limit<(f64, f64)> {
    pub(crate) fn new(point: Limit<Value>) -> Result<Self, Unreachable> {
        let Limit {
            graph_id,
            is_x,
            stream,
        } = point;
        match stream {
            Value::Tuple(point) => match point.iter().collect::<Vec<_>>().as_slice() {
                [Value::Float(min), Value::Float(max)] => Ok(Self {
                    graph_id,
                    is_x,
                    stream: ((*min).into(), (*max).into()),
                }),
                _ => Err(Unreachable::from("Tuple is invalid")),
            },
            _ => Err(Unreachable::from("Point is not a tuple")),
        }
    }
}

#[derive(Debug, Clone)]
pub struct LimitVerdictFactory {
    points: Vec<Limit<StreamReference>>,
}

impl VerdictFactory<TotalIncremental, OutputTime> for LimitVerdictFactory {
    type Error = Infallible;

    type Config = Vec<Limit<StreamReference>>;

    type Rec = Vec<Limit<Value>>;

    fn new(cfg: Self::Config) -> Result<Self, Self::Error> {
        Ok(LimitVerdictFactory { points: cfg })
    }

    fn create(
        &mut self,
        _ts: &<OutputTime as TimeRepresentation>::InnerTime,
        verdict: &TotalIncremental,
    ) -> Result<Self::Rec, Self::Error> {
        Ok(self
            .points
            .iter()
            .flat_map(|point| Limit::<Value>::new(point, verdict))
            .collect())
    }
}

#[derive(Debug, Clone)]
pub struct TriggerVerdictFactory {}

impl VerdictFactory<TotalIncremental, OutputTime> for TriggerVerdictFactory {
    type Error = Infallible;

    type Config = ();

    type Rec = Vec<(<OutputTime as TimeRepresentation>::InnerTime, String)>;

    fn new(_cfg: Self::Config) -> Result<Self, Self::Error> {
        Ok(Self {})
    }

    fn create(
        &mut self,
        ts: &<OutputTime as TimeRepresentation>::InnerTime,
        verdict: &TotalIncremental,
    ) -> Result<Self::Rec, Self::Error> {
        Ok(verdict
            .trigger
            .iter()
            .map(|(_, msg)| (*ts, msg.clone()))
            .collect())
    }
}
