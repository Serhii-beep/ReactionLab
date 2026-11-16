use std::path::Path;

use crate::ChemistryError;
use crate::native_engine::NativeEngine;
use crate::native_operation::NativeOperation;

pub struct ChemistryEngine {
    native: NativeEngine,
}

impl ChemistryEngine {
    pub fn load(library: &Path, elements_json: &str, reference_json: &str) -> Result<Self, ChemistryError> {
        Ok(Self { native: NativeEngine::load(library, elements_json, reference_json)? })
    }

    pub fn parse_formula(&self, formula: &str) -> Result<String, ChemistryError> {
        self.native.answer(NativeOperation::ParseFormula, formula)
    }

    pub fn molar_mass_of(&self, formula: &str) -> Result<String, ChemistryError> {
        self.native.answer(NativeOperation::MolarMassOf, formula)
    }

    pub fn balance(&self, request_json: &str) -> Result<String, ChemistryError> {
        self.native.answer(NativeOperation::Balance, request_json)
    }

    pub fn assemble(&self, request_json: &str) -> Result<String, ChemistryError> {
        self.native.answer(NativeOperation::Assemble, request_json)
    }

    pub fn describe_sketch(&self, request_json: &str) -> Result<String, ChemistryError> {
        self.native.answer(NativeOperation::DescribeSketch, request_json)
    }

    pub fn structure_key(&self, request_json: &str) -> Result<String, ChemistryError> {
        self.native.answer(NativeOperation::StructureKey, request_json)
    }

    pub fn detect_symmetry(&self, request_json: &str) -> Result<String, ChemistryError> {
        self.native.answer(NativeOperation::DetectSymmetry, request_json)
    }

    pub fn predict(&self, request_json: &str) -> Result<String, ChemistryError> {
        self.native.answer(NativeOperation::Predict, request_json)
    }

    pub fn describe_conditions(&self, request_json: &str) -> Result<String, ChemistryError> {
        self.native.answer(NativeOperation::DescribeConditions, request_json)
    }
}
