#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

mod desktop_app;
mod lab_window;

use std::error::Error;
use std::process::ExitCode;

use desktop_app::DesktopApp;
use winit::event_loop::EventLoop;

fn main() -> ExitCode {
    match run() {
        Ok(()) => ExitCode::SUCCESS,
        Err(error) => {
            let mut cause = Some(error.as_ref());

            while let Some(current) = cause {
                eprintln!("{current}");
                cause = current.source();
            }

            ExitCode::FAILURE
        }
    }
}

fn run() -> Result<(), Box<dyn Error>> {
    let event_loop = EventLoop::new()?;
    let mut desktop_app = DesktopApp::default();

    event_loop.run_app(&mut desktop_app)?;

    desktop_app.into_result()
}
