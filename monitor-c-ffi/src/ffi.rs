use crate::error::{FfiError, Unreachable};
use crate::event_source::{EventSource, ParserAST};
use crate::verdict_sink::{
    GraphPoint, GraphVerdictFactory, Limit, LimitVerdictFactory, TriggerVerdictFactory,
    VectorVerdictsSink, VerdictsSink,
};
use crate::{GraphDescriptions, InputTime, OutputTime};
use rtlola_interpreter::input::VectorFactory;
use rtlola_interpreter::rtlola_mir::{Stream, StreamReference};
use rtlola_interpreter::time::TimeRepresentation;
use rtlola_interpreter::Monitor;
use rtlola_interpreter::{monitor::TotalIncremental, Value};
use std::os::raw::c_char;
use std::{convert::Infallible, ffi::CStr};

pub(crate) type Evaluator =
    Monitor<VectorFactory<Infallible, Vec<Value>>, InputTime, TotalIncremental, OutputTime>;

pub struct RTLolaMonitor {
    event_source: EventSource,
    evaluator: Evaluator,
    graph_verdict_sink: VectorVerdictsSink<TotalIncremental, OutputTime, GraphVerdictFactory>,
    trigger_verdict_sink: VectorVerdictsSink<TotalIncremental, OutputTime, TriggerVerdictFactory>,
    limit_verdict_sink: VectorVerdictsSink<TotalIncremental, OutputTime, LimitVerdictFactory>,
}

impl RTLolaMonitor {
    fn new(
        event_source_cfg: ParserAST<String, String>,
        spec_cfg: &str,
        verdict_sink_cfg: Vec<GraphPoint<String>>,
        limit_cfg: Vec<Limit<String>>,
    ) -> Result<Self, FfiError> {
        let config = rtlola_frontend::ParserConfig::for_string(spec_cfg.to_string());
        let handler = rtlola_frontend::Handler::from(config.clone());
        let ir = rtlola_frontend::parse(config).unwrap_or_else(|e| {
            handler.emit_error(&e);
            std::process::exit(1);
        });
        let input_map = ir
            .inputs
            .iter()
            .map(|i| (i.name().to_string(), i.reference.in_ix()))
            .collect();
        let event_source =
            EventSource::new(event_source_cfg, input_map).map_err(FfiError::EventSource)?;
        let evaluator = rtlola_interpreter::ConfigBuilder::new()
            .with_ir(ir)
            .offline::<InputTime>()
            .with_vector_events()
            .with_verdict::<TotalIncremental>()
            .output_time::<OutputTime>()
            .monitor_with_data(event_source.factory.len)
            .expect("Could not build Monitor");
        let points = verdict_sink_cfg
            .into_iter()
            .map(|point| GraphPoint::<StreamReference>::new(point, evaluator.ir()))
            .collect::<Result<_, _>>()
            .map_err(FfiError::InvalidStream)?;
        let points_limit: Vec<Limit<StreamReference>> = limit_cfg
            .into_iter()
            .map(|point| Limit::<StreamReference>::new(point, evaluator.ir()))
            .collect::<Result<_, _>>()
            .map_err(FfiError::InvalidStream)?;
        let graph_verdict_sink = VectorVerdictsSink::new(points).map_err(|_e| {
            FfiError::Unreachable(Unreachable::from("Could not build Graph Factory"))
        })?;
        let trigger_verdict_sink = VectorVerdictsSink::new(()).map_err(|_e| {
            FfiError::Unreachable(Unreachable::from("Could not build Trigger Factory"))
        })?;
        let limit_verdict_sink = VectorVerdictsSink::new(points_limit).map_err(|_e| {
            FfiError::Unreachable(Unreachable::from("Could not build Limit Factory"))
        })?;
        Ok(Self {
            event_source,
            evaluator,
            graph_verdict_sink,
            trigger_verdict_sink,
            limit_verdict_sink,
        })
    }

    pub(crate) fn from_raw(
        event_source_cfg: *const c_char,
        spec_cfg: *const c_char,
        verdict_sink_cfg: GraphDescriptions,
    ) -> Result<Self, FfiError> {
        let event_source_cfg = unsafe { CStr::from_ptr(event_source_cfg) }
            .to_str()
            .map_err(FfiError::Utf8Error)?;
        let event_source_cfg: serde_json::Value =
            serde_json::from_str(event_source_cfg).map_err(|e| {
                FfiError::EventSource(crate::error::EventSourceError::Ast(
                    crate::error::InvalidAst::JsonError(crate::error::JsonError::SyntaxError(e)),
                ))
            })?;
        let event_source_cfg = ParserAST::from_json(event_source_cfg).map_err(|e| {
            FfiError::EventSource(crate::error::EventSourceError::Ast(
                crate::error::InvalidAst::JsonError(e),
            ))
        })?;
        println!("Event source config: {:?}", event_source_cfg);
        let spec_cfg = unsafe { CStr::from_ptr(spec_cfg) }
            .to_str()
            .map_err(FfiError::Utf8Error)?;

        let verdict_sink_cfg_save = verdict_sink_cfg.clone();
        let verdict_sink_cfg: Vec<GraphPoint<String>> = verdict_sink_cfg.try_into()?;
        println!("graph point strings: {:?}", verdict_sink_cfg);
        let limit_sink_cfg: Vec<Limit<String>> = verdict_sink_cfg_save.try_into()?;
        // let verdict_sink_cfg = vec![
        //     GraphPoint::<String>::new(0, 0, "velocity".to_string()),
        //     GraphPoint::<String>::new(0, 1, "acceleration".to_string()),
        //     GraphPoint::<String>::new(1, 0, "gps".to_string()),
        // ];
        // let event_source_cfg = ParserAST::Seq(vec![
        //     ParserAST::Element {
        //         stream: "msg_id",
        //         ty: event_source::Type::UInt8,
        //     },
        //     ParserAST::Split {
        //         switch: "msg_id",
        //         ty: vec![
        //             (
        //                 0,
        //                 ParserAST::Seq(vec![
        //                     ParserAST::Element {
        //                         stream: "velocity_x",
        //                         ty: event_source::Type::UInt32,
        //                     },
        //                     ParserAST::Element {
        //                         stream: "velocity_y",
        //                         ty: event_source::Type::UInt32,
        //                     },
        //                 ]),
        //             ),
        //             (
        //                 1,
        //                 ParserAST::Seq(vec![
        //                     ParserAST::Element {
        //                         stream: "acceleration_x",
        //                         ty: event_source::Type::UInt16,
        //                     },
        //                     ParserAST::Element {
        //                         stream: "acceleration_y",
        //                         ty: event_source::Type::UInt16,
        //                     },
        //                 ]),
        //             ),
        //             (
        //                 2,
        //                 ParserAST::Seq(vec![
        //                     ParserAST::Element {
        //                         stream: "latitude",
        //                         ty: event_source::Type::UInt64,
        //                     },
        //                     ParserAST::Element {
        //                         stream: "longitude",
        //                         ty: event_source::Type::UInt64,
        //                     },
        //                 ]),
        //             ),
        //         ]
        //         .into_iter()
        //         .collect(),
        //     },
        // ]);
        Self::new(event_source_cfg, spec_cfg, verdict_sink_cfg, limit_sink_cfg)
    }

    pub(crate) fn accept_event(
        &mut self,
        bytes: &[u8],
        ts: <InputTime as TimeRepresentation>::InnerTime,
    ) -> Result<
        (
            usize,
            Vec<GraphPoint<Value>>,
            Vec<(<OutputTime as TimeRepresentation>::InnerTime, String)>,
            Vec<Limit<Value>>,
        ),
        FfiError,
    > {
        let (event, num_bytes) = self
            .event_source
            .get_event(bytes)
            .map_err(FfiError::EventSource)?;
        //println!("generated event");
        let verdicts = self
            .evaluator
            .accept_event(event, ts)
            .map_err(|e| FfiError::EventSource(crate::error::EventSourceError::Factory(e)))?;
        //println!("generated verdicts");
        let graph_points = self
            .graph_verdict_sink
            .sink(&verdicts)
            .map_err(|_e| {
                FfiError::Unreachable(Unreachable::from("Could not build Graph Verdict"))
            })?
            .into_iter()
            .flatten()
            .collect();
        //println!("generated points: ");
        let limits: Vec<Limit<Value>> = self
            .limit_verdict_sink
            .sink(&verdicts)
            .map_err(|_e| {
                FfiError::Unreachable(Unreachable::from("Could not build Graph Verdict"))
            })?
            .into_iter()
            .flatten()
            .collect();
        //println!("generated limits");
        let triggers = self
            .trigger_verdict_sink
            .sink(&verdicts)
            .map_err(|_e| {
                FfiError::Unreachable(Unreachable::from("Could not build Trigger Verdict"))
            })?
            .into_iter()
            .flatten()
            .collect();
        //println!("generated trigger");
        Ok((num_bytes, graph_points, triggers, limits))
    }
}
