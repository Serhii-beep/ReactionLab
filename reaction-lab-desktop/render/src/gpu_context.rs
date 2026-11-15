use crate::GpuSetupError;

pub struct GpuContext {
    pub(crate) instance: wgpu::Instance,
    pub(crate) adapter: wgpu::Adapter,
    pub(crate) device: wgpu::Device,
    pub(crate) queue: wgpu::Queue,
}

impl GpuContext {
    pub async fn new(
        instance: wgpu::Instance,
        compatible_surface: Option<&wgpu::Surface<'_>>,
    ) -> Result<Self, GpuSetupError> {
        let adapter = instance
            .request_adapter(&wgpu::RequestAdapterOptions {
                power_preference: wgpu::PowerPreference::HighPerformance,
                compatible_surface,
                ..Default::default()
            })
            .await?;
        let required_limits = wgpu::Limits {
            max_texture_dimension_2d: adapter.limits().max_texture_dimension_2d,
            ..wgpu::Limits::default()
        };
        let (device, queue) = adapter
            .request_device(&wgpu::DeviceDescriptor {
                label: Some("ReactionLab"),
                required_limits,
                ..Default::default()
            })
            .await?;

        Ok(Self { instance, adapter, device, queue })
    }

    pub fn create_surface(
        &self,
        target: impl Into<wgpu::SurfaceTarget<'static>>,
    ) -> Result<wgpu::Surface<'static>, wgpu::CreateSurfaceError> {
        self.instance.create_surface(target)
    }
}
