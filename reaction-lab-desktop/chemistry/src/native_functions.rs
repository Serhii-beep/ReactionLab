use std::ffi::c_void;
use std::path::Path;
use std::slice;

use libloading::Library;

use crate::ChemistryError;
use crate::native_text::NativeText;

const INTERFACE_VERSION: i32 = 1;

type InterfaceVersion = unsafe extern "C" fn() -> i32;
type LoadEngine = unsafe extern "C" fn(*const u8, usize, *const u8, usize, *mut *mut c_void, *mut NativeText) -> i32;
type FreeEngine = unsafe extern "C" fn(*mut c_void);
type FreeText = unsafe extern "C" fn(*mut u8);
type Operation = unsafe extern "C" fn(*const u8, usize, *mut NativeText) -> i32;
type EngineOperation = unsafe extern "C" fn(*mut c_void, *const u8, usize, *mut NativeText) -> i32;

#[derive(Clone, Copy)]
pub(crate) struct NativeFunctions {
    pub(crate) load_engine: LoadEngine,
    pub(crate) free_engine: FreeEngine,
    pub(crate) parse_formula: Operation,
    pub(crate) molar_mass_of: EngineOperation,
    pub(crate) balance: Operation,
    pub(crate) assemble: EngineOperation,
    pub(crate) describe_sketch: EngineOperation,
    pub(crate) structure_key: Operation,
    pub(crate) detect_symmetry: Operation,
    pub(crate) predict: EngineOperation,
    pub(crate) describe_conditions: EngineOperation,
    free_text: FreeText,
}

impl NativeFunctions {
    pub(crate) fn open(path: &Path) -> Result<Self, ChemistryError> {
        // SAFETY: the library is the engine built by NativeAOT, whose loading runs no code of ours.
        let library = unsafe { Library::new(path)? };
        // .NET can't unload a NativeAOT library while its runtime runs, so it stays loaded until the process exits.
        let library: &'static Library = Box::leak(Box::new(library));
        // SAFETY: the export takes nothing and returns the version of the interface the library was built with.
        let found = unsafe { library.get::<InterfaceVersion>(c"chemistry_interface_version")?() };

        if found != INTERFACE_VERSION {
            return Err(ChemistryError::Interface { found, expected: INTERFACE_VERSION });
        }

        // SAFETY: each name is one of the library's exports, each type matches its C signature in this version of
        // the interface, and the library is never unloaded, so the pointers stay valid.
        unsafe {
            Ok(Self {
                load_engine: *library.get::<LoadEngine>(c"chemistry_engine_load")?,
                free_engine: *library.get::<FreeEngine>(c"chemistry_engine_free")?,
                parse_formula: *library.get::<Operation>(c"chemistry_parse_formula")?,
                molar_mass_of: *library.get::<EngineOperation>(c"chemistry_molar_mass_of")?,
                balance: *library.get::<Operation>(c"chemistry_balance")?,
                assemble: *library.get::<EngineOperation>(c"chemistry_assemble")?,
                describe_sketch: *library.get::<EngineOperation>(c"chemistry_describe_sketch")?,
                structure_key: *library.get::<Operation>(c"chemistry_structure_key")?,
                detect_symmetry: *library.get::<Operation>(c"chemistry_detect_symmetry")?,
                predict: *library.get::<EngineOperation>(c"chemistry_predict")?,
                describe_conditions: *library.get::<EngineOperation>(c"chemistry_describe_conditions")?,
                free_text: *library.get::<FreeText>(c"chemistry_text_free")?,
            })
        }
    }

    pub(crate) fn take_text(&self, text: NativeText) -> String {
        if text.bytes.is_null() {
            return String::new();
        }

        // SAFETY: the library allocated `length` bytes at `bytes`, and they stay valid until `free_text`.
        let owned = unsafe { String::from_utf8_lossy(slice::from_raw_parts(text.bytes, text.length)).into_owned() };
        // SAFETY: the bytes came from the library and are freed only here.
        unsafe { (self.free_text)(text.bytes) };

        owned
    }
}
