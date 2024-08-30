# Monitor-C-FFI
This project includes the interface built for the [RTLola monitor](https://crates.io/crates/rtlola-interpreter) for the RTLolaMo<sup>3</sup>Vis app.

We also include some example specifications and configurations to use in the app here.

## How-To Build
This project is meant to be used together with the RTLolaMo<sup>3</sup>Vis. To use both projects in combination, this project needs to be compiled to a dynamic-linked library (`.dylib`) for Android, or a fat library (`.a`) for iOS. Since most of our experiments are done using iOS, we will detail the build process and some common problems for iOS here.

### cargo lipo
To build the `.a` file, we have previously used `cargo lipo` (see [here](https://docs.rs/crate/cargo-lipo/3.1.0)).

Depending on whether a debug or a release build is necessary, use either 
```
cargo lipo
```

to build the files, or

```
cargo lipo --release
``` 
Then, the commands create different folders for the different iOS architectures. For example, since we often used an iPhone SE in our experiments, we included the corresponding `.a` file for ARM64 from the folder `aarch64-apple-ios` that cargo lipo creates.


## Important functions
Here, we include a brief overview of every function visible to the RTLolaMo<sup>3</sup>Vis app. These functions are used in the app to interact with the RTLola monitor.

### accept_event
```Rust
accept_event(mon: *mut RTLolaMonitor,
    bytes: *mut u8,
    len: usize,
    ts: Duration,) -> *mut Verdicts
```

The `accept_event` function accepts a pointer to the RTLola monitor, a byte array, the size of that byte array, and a duration.

The RTLola monitor interprets the passed byte array as input to the currently configured specification. Thus, it must contain data that is compatible with the defined event source configuration. 
The duration is used as the timestamp when the input data arrived during interpretation.

It produces a `Verdict` object that contains all points for the visualization, all triggers, and potentially log objects that are treated as triggers in the app but contain error messages produced during interpretation.

This function is called by the app to pass on input data and receive the corresponding result from the RTLola Interpreter.

### new_monitor
```Rust
new_monitor(
    event_source_cfg: *const c_char,
    spec_cfg: *const c_char,
    verdict_sink_cfg: GraphDescriptions,
) -> MonitorResult
```

The `new_monitor` function build a new monitor object that is passed to the app and later used to call `accept_event` on.

It receives an event source configuration, a specification, and a verdict sink configuration as input from the app (how these can be created and what they configure is explained in our examples and in our paper).

The app is then returned the `MonitorResult` which either contains whatever error building the monitor with these configurations produced or the monitor object.
If an error occurred, it is visualized by the app.

### free_monitor
```Rust
free_monitor(monitor_ptr: *mut RTLolaMonitor)
```

The `free_monitor` function is called by the app when the user wants to destroy the current monitor object.

Since the monitor object is handled by the FFI and not owned by the app, it also needs to be destroyed by the FFI. Thus, whenever an app user wants to create a new monitor object or is finished using the monitor, this function should be called.

## Examples

As in the paper we also include three examples here. They are in the folder `examples`.

### Examples 1 & 2: Drone Flight
Both example 1 and example 2 are based on a specification that monitors the flight of a drone. This specification is given in `spec_exp_drone.lola`.
Examples 1 and 2 contain different event source configuration. Example 1 includes a simple event source configurations with only one payload definition as given in `esc_drone_1.json`. For example 2, we use the same specification and thus input streams, but they are divided into two possible payloads. This is defined in `esc_drone_2.json`.
Lastly, the visualization of these examples is defined in `vsc_exp_drone.json`. It produces three plots in the app.

### Example 3: Checking for IP-addresses
The example specification in `spec_exp_ip.lola`checks for the occurrence of four randomly chosen IP addresses. 
The event source configuration in `esc_exp_ip.json` defines that the monitor should only accept packets that include values for all input streams.
The verdict sink configuration `vsc_exp_ip.json` specifies that all four output streams are plotted in a single plot.


## Copyright
Copyright (C) CISPA - Helmholtz Center for Information Security 2024. Authors: Jan Baumeister, Jan Kautenburger, and Clara Rubeck. Based on original work at Universität des Saarlandes (C) 2024. Authors: Jan Baumeister, Jan Kautenburger, and Clara Rubeck.