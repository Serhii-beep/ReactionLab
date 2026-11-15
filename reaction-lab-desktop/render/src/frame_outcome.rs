#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum FrameOutcome {
    Presented,
    Skipped,
    Reconfigured,
    SurfaceLost,
}
