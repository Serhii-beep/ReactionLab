export interface Element {
    id: string;
    atomicNumber: number;
    symbol: string;
    name: string;
    atomicMass: number;
    category: ElementCategory;
    period: number;
    group: number | null;
    electronConfiguration: string | null;
    electronegativity: number | null;
    atomicRadius: number | null;
    ionizationEnergy: number | null;
    meltingPoint: number | null;
    boilingPoint: number | null;
    density: number | null;
    color: string | null;
    stateAtRoomTemp: MatterState;
    displayColor: string;
    radius3D: number;
    discoveryInfo: string | null;
    interestingFacts: string | null;
}

export interface ElementSummary {
    id: string;
    atomicNumber: number;
    symbol: string;
    name: string;
    atomicMass: number;
    category: ElementCategory;
    period: number;
    group: number | null;
    stateAtRoomTemp: MatterState;
    displayColor: string;
}

export enum ElementCategory {
    AlkaliMetal,
    AlkalineEarthMetal,
    TransitionMetal,
    PostTransitionMetal,
    Metalloid,
    NonMetal,
    Halogen,
    NobleGas,
    Lanthanide,
    Actinide,
    Unknown
}

export enum MatterState {
    Solid,
    Liquid,
    Gas,
    Aqueous,
    Plasma
}