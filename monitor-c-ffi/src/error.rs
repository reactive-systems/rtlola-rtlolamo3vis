use rtlola_interpreter::input::EventFactoryError;

#[derive(Debug)]
/// FFI Error when using the RTLola FFI
pub enum FfiError {
    Utf8Error(core::str::Utf8Error),
    InvalidStream(StreamError),
    Unreachable(Unreachable),
    EventSource(EventSourceError),
}

impl std::fmt::Display for FfiError {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            FfiError::Utf8Error(e) => write!(f, "{e}"),
            FfiError::InvalidStream(e) => write!(f, "{e}"),
            FfiError::Unreachable(e) => write!(f, "{e}"),
            FfiError::EventSource(e) => write!(f, "{e}"),
        }
    }
}

impl std::error::Error for FfiError {
    fn source(&self) -> Option<&(dyn std::error::Error + 'static)> {
        match self {
            FfiError::Utf8Error(e) => Some(e),
            FfiError::InvalidStream(e) => Some(e),
            FfiError::Unreachable(e) => Some(e),
            FfiError::EventSource(e) => Some(e),
        }
    }
}

#[derive(Debug, Clone)]
pub enum StreamError {
    StreamNotFound(String),
    InvalidType(String),
}

impl std::fmt::Display for StreamError {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            StreamError::StreamNotFound(stream) => {
                write!(f, "Could not find stream {stream} in Specification")
            }
            StreamError::InvalidType(stream) => {
                write!(f, "Invalid Type for stream {stream} in Specification")
            }
        }
    }
}

impl std::error::Error for StreamError {}

#[derive(Debug, Clone)]
pub struct Unreachable(String);

impl<'a> From<&'a str> for Unreachable {
    fn from(value: &'a str) -> Self {
        let inner = value.to_string();
        Unreachable(inner)
    }
}

impl std::fmt::Display for Unreachable {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(f, "Should be unreachable but got: {}", self.0)
    }
}

impl std::error::Error for Unreachable {}

#[derive(Debug)]
pub enum InvalidAst {
    SplitKeyNotFound(String),
    ParseError(ParseError),
    JsonError(JsonError),
}

impl std::fmt::Display for InvalidAst {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            InvalidAst::SplitKeyNotFound(stream) => {
                write!(f, "Could not find stream {stream} in ParseTree")
            }
            InvalidAst::ParseError(e) => write!(f, "Parsing Error ({e})",),
            InvalidAst::JsonError(e) => write!(f, "Invalid Json File ({e})"),
        }
    }
}

impl std::error::Error for InvalidAst {}

#[derive(Debug)]
pub enum JsonError {
    SyntaxError(serde_json::Error),
    InvalidStart,
    InvalidType(TypeError),
    InvalidPayload,
    InvalidJsonValue,
}

impl std::fmt::Display for JsonError {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            JsonError::SyntaxError(e) => write!(f, "{}", e),
            JsonError::InvalidStart => write!(f, "Json file needs to start with an Object"),
            JsonError::InvalidType(e) => writeln!(f, "{}", e),
            JsonError::InvalidPayload => write!(
                f,
                "Payload Descriptions are an object containing an payload_id and data_objects"
            ),
            JsonError::InvalidJsonValue => write!(f, "Only strings and objects are supported"),
        }
    }
}

impl std::error::Error for JsonError {
    fn source(&self) -> Option<&(dyn std::error::Error + 'static)> {
        match self {
            JsonError::InvalidStart | JsonError::InvalidPayload | JsonError::InvalidJsonValue => {
                None
            }
            JsonError::InvalidType(e) => Some(e),
            JsonError::SyntaxError(e) => Some(e),
        }
    }
}

#[derive(Debug, Clone)]
pub struct TypeError(pub String);

impl std::fmt::Display for TypeError {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(f, "Invalid Type: {}", self.0)
    }
}

impl std::error::Error for TypeError {}

#[derive(Debug)]
pub enum ParseError {
    Unreachable(Unreachable),
    ValueConversion(nom::error::Error<Vec<u8>>),
    InvalidSplitKey { switch_counter: usize, value: u64 },
}

impl std::fmt::Display for ParseError {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            ParseError::Unreachable(e) => write!(f, "{e}"),
            ParseError::ValueConversion(e) => write!(f, "{e:?}",),
            ParseError::InvalidSplitKey {
                switch_counter,
                value,
            } => write!(f, "Invalid switch value {value} for key {switch_counter}"),
        }
    }
}

impl std::error::Error for ParseError {}

#[derive(Debug)]
pub enum EventSourceError {
    Ast(InvalidAst),
    Factory(EventFactoryError),
    Incomplete,
}

impl std::fmt::Display for EventSourceError {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            EventSourceError::Ast(e) => write!(f, "{e}"),
            EventSourceError::Factory(e) => write!(f, "{e}"),
            EventSourceError::Incomplete => write!(f, "Stream Buffer insufficient"),
        }
    }
}

impl std::error::Error for EventSourceError {
    fn source(&self) -> Option<&(dyn std::error::Error + 'static)> {
        match self {
            EventSourceError::Incomplete => None,
            EventSourceError::Ast(ast) => Some(ast),
            EventSourceError::Factory(fac) => Some(fac),
        }
    }
}
