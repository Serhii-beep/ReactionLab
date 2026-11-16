use std::ptr;

#[repr(C)]
pub(crate) struct NativeText {
    pub(crate) bytes: *mut u8,
    pub(crate) length: usize,
}

impl NativeText {
    pub(crate) fn empty() -> Self {
        Self { bytes: ptr::null_mut(), length: 0 }
    }
}
