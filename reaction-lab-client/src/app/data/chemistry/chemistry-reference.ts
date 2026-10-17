export interface AtomicGeometryEntry {
    readonly symbol: string;
    readonly valenceElectrons: number;
    readonly single: number;
    readonly double?: number;
    readonly triple?: number;
    readonly aromatic?: number;
}

export interface AtomicGeometryDataset {
    readonly elements: readonly AtomicGeometryEntry[];
}

export interface ChemistryReference {
    readonly 'atomic-geometry': AtomicGeometryDataset;
}