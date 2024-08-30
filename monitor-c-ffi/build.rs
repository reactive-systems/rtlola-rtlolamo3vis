extern crate cbindgen;

use std::env;
use std::path::Path;

fn main() {
    let crate_dir = env::var("CARGO_MANIFEST_DIR").unwrap();

    let header_path = Path::new("target/rtlola.h");
    cbindgen::generate(crate_dir)
        .unwrap()
        .write_to_file(header_path);
}
