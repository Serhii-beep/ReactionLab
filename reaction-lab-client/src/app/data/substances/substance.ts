export type SubstanceKind = 'Molecular' | 'Ionic' | 'Metallic' | 'Monatomic' | 'NetworkCovalent';

export type MatterState = 'Solid' | 'Liquid' | 'Gas' | 'Aqueous' | 'Plasma';

export type BondType = 'Single' | 'Double' | 'Triple' | 'Aromatic' | 'Ionic' | 'Hydrogen' | 'Metallic'

export interface SubstanceSummary {
    readonly id: string;
    readonly formula: string;
    readonly name: string;
    readonly kind: SubstanceKind;
    readonly isOrganic: boolean;
    readonly stateAtRoomTemperature: MatterState;
    readonly weightGramsPerMole: number | null;
    readonly category: string | null;
}

export interface StructureAtom {
    readonly symbol: string;
    readonly x: number;
    readonly y: number;
    readonly z: number;
}

export interface StructureBond {
    readonly fromAtomIndex: number;
    readonly toAtomIndex: number;
    readonly type: BondType;
}

export interface MolecularStructure {
    readonly atoms: readonly StructureAtom[];
    readonly bonds: readonly StructureBond[];
}

export interface SubstanceDetail extends SubstanceSummary {
    readonly hillFormula: string;
    readonly structure: MolecularStructure | null;
}