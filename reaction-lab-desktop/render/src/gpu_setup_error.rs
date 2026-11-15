#[derive(Debug, thiserror::Error)]
pub enum GpuSetupError {
    #[error("No GPU can draw to this window.")]
    NoAdapter(#[from] wgpu::RequestAdapterError),
    #[error("The GPU could not create a device.")]
    NoDevice(#[from] wgpu::RequestDeviceError),
}
