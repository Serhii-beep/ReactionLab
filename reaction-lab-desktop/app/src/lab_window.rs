use std::error::Error;
use std::sync::Arc;

use reaction_lab_render::{FrameOutcome, GpuContext, SceneSurface, ThemeColors, wgpu};
use winit::dpi::{LogicalSize, PhysicalSize};
use winit::event_loop::ActiveEventLoop;
use winit::window::{Theme, Window};

pub struct LabWindow {
    window: Arc<Window>,
    gpu: GpuContext,
    scene_surface: SceneSurface,
    theme: Theme,
}

impl LabWindow {
    pub fn open(event_loop: &ActiveEventLoop) -> Result<Self, Box<dyn Error>> {
        let attributes = Window::default_attributes()
            .with_title("ReactionLab")
            .with_inner_size(LogicalSize::new(1280.0, 800.0))
            .with_visible(false);
        let window = Arc::new(event_loop.create_window(attributes)?);
        let display = Box::new(event_loop.owned_display_handle());
        let instance = wgpu::Instance::new(wgpu::InstanceDescriptor::new_with_display_handle_from_env(display));
        let surface = instance.create_surface(Arc::clone(&window))?;
        let gpu = pollster::block_on(GpuContext::new(instance, Some(&surface)))?;
        let size = window.inner_size();
        let scene_surface = SceneSurface::new(surface, &gpu, size.width, size.height);
        let theme = window.theme().unwrap_or(Theme::Light);
        let mut lab_window = Self { window, gpu, scene_surface, theme };

        lab_window.draw()?;
        lab_window.show_maximized();

        Ok(lab_window)
    }

    pub fn resize(&mut self, size: PhysicalSize<u32>) {
        if size.width == 0 || size.height == 0 {
            return;
        }

        self.scene_surface.resize(&self.gpu, size.width, size.height);
        self.window.request_redraw();
    }

    pub fn change_theme(&mut self, theme: Theme) {
        self.theme = theme;
        self.window.request_redraw();
    }

    pub fn request_redraw(&self) {
        self.window.request_redraw();
    }

    pub fn draw(&mut self) -> Result<(), Box<dyn Error>> {
        let size = self.window.inner_size();

        if size.width == 0 || size.height == 0 {
            return Ok(());
        }

        let colors = match self.theme {
            Theme::Light => &ThemeColors::LIGHT,
            Theme::Dark => &ThemeColors::DARK,
        };

        match self.scene_surface.draw_background(&self.gpu, colors.surface_base) {
            FrameOutcome::Presented | FrameOutcome::Skipped => {}
            FrameOutcome::Reconfigured => self.window.request_redraw(),
            FrameOutcome::SurfaceLost => {
                let surface = self.gpu.create_surface(Arc::clone(&self.window))?;

                self.scene_surface = SceneSurface::new(surface, &self.gpu, size.width, size.height);
                self.window.request_redraw();
            }
        }

        Ok(())
    }

    fn show_maximized(&self) {
        self.window.set_maximized(true);
        self.window.set_visible(true);
        self.window.set_maximized(true);
        self.window.request_redraw();
    }
}
