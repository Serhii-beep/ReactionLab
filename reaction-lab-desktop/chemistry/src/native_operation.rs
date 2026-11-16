#[derive(Clone, Copy)]
pub(crate) enum NativeOperation {
    ParseFormula,
    MolarMassOf,
    Balance,
    Assemble,
    DescribeSketch,
    StructureKey,
    DetectSymmetry,
    Predict,
    DescribeConditions,
}
