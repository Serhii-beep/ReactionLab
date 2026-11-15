use crate::{FrameOutcome, GpuContext};

pub struct SceneSurface {
    surface: wgpu::Surface<'static>,
    configuration: wgpu::SurfaceConfiguration,
    view_format: wgpu::TextureFormat,
}

impl SceneSurface {
    pub fn new(surface: wgpu::Surface<'static>, gpu: &GpuContext, width: u32, height: u32) -> Self {
        let capabilities = surface.get_capabilities(&gpu.adapter);
        let format =
            capabilities.formats.iter().copied().find(wgpu::TextureFormat::is_srgb).unwrap_or(capabilities.formats[0]);
        let view_format = format.add_srgb_suffix();
        let largest_side = gpu.device.limits().max_texture_dimension_2d;
        let configuration = wgpu::SurfaceConfiguration {
            usage: wgpu::TextureUsages::RENDER_ATTACHMENT,
            format,
            color_space: wgpu::SurfaceColorSpace::Auto,
            width: width.clamp(1, largest_side),
            height: height.clamp(1, largest_side),
            desired_maximum_frame_latency: 2,
            present_mode: wgpu::PresentMode::AutoVsync,
            alpha_mode: wgpu::CompositeAlphaMode::Auto,
            view_formats: vec![view_format],
        };

        surface.configure(&gpu.device, &configuration);

        Self { surface, configuration, view_format }
    }

    pub fn resize(&mut self, gpu: &GpuContext, width: u32, height: u32) {
        if width == 0 || height == 0 {
            return;
        }

        let largest_side = gpu.device.limits().max_texture_dimension_2d;

        self.configuration.width = width.min(largest_side);
        self.configuration.height = height.min(largest_side);
        self.surface.configure(&gpu.device, &self.configuration);
    }

    pub fn draw_background(&self, gpu: &GpuContext, color: wgpu::Color) -> FrameOutcome {
        let (frame, suboptimal) = match self.surface.get_current_texture() {
            wgpu::CurrentSurfaceTexture::Success(frame) => (frame, false),
            wgpu::CurrentSurfaceTexture::Suboptimal(frame) => (frame, true),
            wgpu::CurrentSurfaceTexture::Outdated => {
                self.surface.configure(&gpu.device, &self.configuration);
                return FrameOutcome::Reconfigured;
            }
            wgpu::CurrentSurfaceTexture::Lost => return FrameOutcome::SurfaceLost,
            wgpu::CurrentSurfaceTexture::Timeout
            | wgpu::CurrentSurfaceTexture::Occluded
            | wgpu::CurrentSurfaceTexture::Validation => return FrameOutcome::Skipped,
        };
        let view = frame
            .texture
            .create_view(&wgpu::TextureViewDescriptor { format: Some(self.view_format), ..Default::default() });
        let mut encoder = gpu.device.create_command_encoder(&wgpu::CommandEncoderDescriptor::default());

        encoder.begin_render_pass(&wgpu::RenderPassDescriptor {
            label: Some("background"),
            color_attachments: &[Some(wgpu::RenderPassColorAttachment {
                view: &view,
                depth_slice: None,
                resolve_target: None,
                ops: wgpu::Operations { load: wgpu::LoadOp::Clear(color), store: wgpu::StoreOp::Store },
            })],
            ..Default::default()
        });
        gpu.queue.submit([encoder.finish()]);
        gpu.queue.present(frame);

        if suboptimal {
            self.surface.configure(&gpu.device, &self.configuration);
            return FrameOutcome::Reconfigured;
        }

        FrameOutcome::Presented
    }
}
