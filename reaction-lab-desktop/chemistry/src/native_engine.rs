use std::ffi::c_void;
use std::path::Path;
use std::ptr::{self, NonNull};

use crate::ChemistryError;
use crate::native_functions::NativeFunctions;
use crate::native_operation::NativeOperation;
use crate::native_text::NativeText;

const SUCCEEDED: i32 = 0;

pub(crate) struct NativeEngine {
    functions: NativeFunctions,
    handle: NonNull<c_void>,
}

impl NativeEngine {
    pub(crate) fn load(library: &Path, elements_json: &str, reference_json: &str) -> Result<Self, ChemistryError> {
        let functions = NativeFunctions::open(library)?;
        let mut handle = ptr::null_mut();
        let mut failure = NativeText::empty();
        // SAFETY: both inputs are UTF-8 of the given lengths, borrowed for the call, and the library writes either
        // the engine's handle or the failure's text.
        let status = unsafe {
            (functions.load_engine)(
                elements_json.as_ptr(),
                elements_json.len(),
                reference_json.as_ptr(),
                reference_json.len(),
                &mut handle,
                &mut failure,
            )
        };

        match NonNull::new(handle) {
            Some(handle) if status == SUCCEEDED => Ok(Self { functions, handle }),
            _ => Err(ChemistryError::Engine(functions.take_text(failure))),
        }
    }

    pub(crate) fn answer(&self, operation: NativeOperation, input: &str) -> Result<String, ChemistryError> {
        let functions = &self.functions;
        let engine = self.handle.as_ptr();
        let (bytes, length) = (input.as_ptr(), input.len());
        let mut text = NativeText::empty();
        // SAFETY: `input` is UTF-8 of the given length, borrowed for the call, `engine` is alive until `drop`, and
        // the library writes its answer into `text`.
        let status = unsafe {
            match operation {
                NativeOperation::ParseFormula => (functions.parse_formula)(bytes, length, &mut text),
                NativeOperation::MolarMassOf => (functions.molar_mass_of)(engine, bytes, length, &mut text),
                NativeOperation::Balance => (functions.balance)(bytes, length, &mut text),
                NativeOperation::Assemble => (functions.assemble)(engine, bytes, length, &mut text),
                NativeOperation::DescribeSketch => (functions.describe_sketch)(engine, bytes, length, &mut text),
                NativeOperation::StructureKey => (functions.structure_key)(bytes, length, &mut text),
                NativeOperation::DetectSymmetry => (functions.detect_symmetry)(bytes, length, &mut text),
                NativeOperation::Predict => (functions.predict)(engine, bytes, length, &mut text),
                NativeOperation::DescribeConditions => {
                    (functions.describe_conditions)(engine, bytes, length, &mut text)
                }
            }
        };
        let answer = functions.take_text(text);

        if status == SUCCEEDED { Ok(answer) } else { Err(ChemistryError::Engine(answer)) }
    }
}

impl Drop for NativeEngine {
    fn drop(&mut self) {
        // SAFETY: the handle came from `chemistry_engine_load` and is freed only here.
        unsafe { (self.functions.free_engine)(self.handle.as_ptr()) }
    }
}

// SAFETY: the handle isn't tied to the thread that loaded it: .NET attaches whichever thread calls into the library,
// and the engine keeps no per-thread state.
unsafe impl Send for NativeEngine {}
