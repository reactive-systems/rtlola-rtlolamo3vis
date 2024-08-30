#include <stdint.h>
/// @brief Monitor containing the Rust_Monitor, EventSource, and VerdictSink
struct Monitor
{
};

struct Duration
{
    double time_in_secs;
};

struct GraphVerdict
{
    uint64_t timestamp;
    uint8_t graph_id;
    uint8_t line_id;
    double x_coord;
    double y_coord;
    bool triggered;
};

struct GraphVerdicts
{
    uint8_t type; // boundary
    uint8_t num_parsed_bytes;
    uint8_t num_verdict;
    GraphVerdict *verdicts;
};

/// @brief Creates a new Monitor
/// @param event_source_cfg Content of a json file describing the content of the received bytes
/// @param spec_cfg A RTLola specification
/// @param verdict_sink_cfg Desciption of the Graphs
/// @return A new RTLola Monitor
Monitor *new_monitor(char *event_source_cfg, char *spec_cfg, char *verdict_sink_cfg);

/// @brief Accepts incoming bytes, parses an event, gives this event to the monitor and prepares the verdicts to a GraphVerdicts
/// @param bytes
/// @param ts
/// @return Null if the bytes are insufficent to create the event
GraphVerdicts(*) accept_event(Monitor *mon, uint8_t *bytes, Duration ts);

/// @brief  Frees the monitor
/// @param mon
void free_monitor(Monitor *mon);

/// @brief Frees GraphVerdicts
void free_verdicts(GraphVerdicts *verdicts);