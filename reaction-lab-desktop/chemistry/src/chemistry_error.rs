#[derive(Debug, thiserror::Error)]
pub enum ChemistryError {
    #[error("The chemistry engine's library could not be loaded.")]
    Library(#[from] libloading::Error),
    #[error("The chemistry engine's library has interface version {found}, but this build needs {expected}.")]
    Interface { found: i32, expected: i32 },
    #[error("{0}")]
    Engine(String),
}
