use std::error::Error;

use winit::application::ApplicationHandler;
use winit::event::WindowEvent;
use winit::event_loop::ActiveEventLoop;
use winit::window::WindowId;

use crate::lab_window::LabWindow;

#[derive(Default)]
pub struct DesktopApp {
    lab_window: Option<LabWindow>,
    fatal_error: Option<Box<dyn Error>>,
}

impl DesktopApp {
    pub fn into_result(self) -> Result<(), Box<dyn Error>> {
        match self.fatal_error {
            Some(error) => Err(error),
            None => Ok(()),
        }
    }

    fn stop(&mut self, event_loop: &ActiveEventLoop, error: Box<dyn Error>) {
        self.fatal_error = Some(error);
        event_loop.exit();
    }
}

impl ApplicationHandler for DesktopApp {
    fn resumed(&mut self, event_loop: &ActiveEventLoop) {
        if self.lab_window.is_some() {
            return;
        }

        match LabWindow::open(event_loop) {
            Ok(lab_window) => self.lab_window = Some(lab_window),
            Err(error) => self.stop(event_loop, error),
        }
    }

    fn window_event(&mut self, event_loop: &ActiveEventLoop, _window_id: WindowId, event: WindowEvent) {
        let Some(lab_window) = self.lab_window.as_mut() else {
            return;
        };

        match event {
            WindowEvent::CloseRequested => event_loop.exit(),
            WindowEvent::Resized(size) => lab_window.resize(size),
            WindowEvent::ThemeChanged(theme) => lab_window.change_theme(theme),
            WindowEvent::Occluded(false) => lab_window.request_redraw(),
            WindowEvent::RedrawRequested => {
                if let Err(error) = lab_window.draw() {
                    self.stop(event_loop, error);
                }
            }
            _ => {}
        }
    }
}
