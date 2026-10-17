export interface FormulaResult {
    readonly hill?: string;
    readonly charge: number;
    readonly countBySymbol?: Readonly<Record<string, number>>;
    readonly error?: string;
}

export interface MolarMassResult {
    readonly gramsPerMole?: number;
    readonly error?: string;
    readonly unknownSymbol?: string;
}

export interface BalanceResult {
    readonly reactantCoefficients?: readonly number[];
    readonly productCoefficients?: readonly number[];
    readonly error?: string;
    readonly invalidFormula?: string;
}

export interface AtomEntry {
    readonly symbol: string;
    readonly formalCharge: number;
}

export interface BondEntry {
    readonly from: number;
    readonly to: number;
    readonly order: number;
}

export interface AssemblyResult {
    readonly positionsAngstroms?: readonly number[];
    readonly error?: string;
}

export type ChemistryRequest =
    | { readonly operation: 'loadReferenceData'; readonly atomicMassesJson: string; readonly atomicGeometryJson: string }
    | { readonly operation: 'parseFormula'; readonly formula: string }
    | { readonly operation: 'molarMassOf'; readonly formula: string }
    | { readonly operation: 'balance'; readonly requestJson: string }
    | { readonly operation: 'assemble'; readonly requestJson: string };

export interface ChemistryResultByOperation {
    readonly parseFormula: FormulaResult;
    readonly molarMassOf: MolarMassResult;
    readonly balance: BalanceResult;
    readonly assemble: AssemblyResult;
}

export type ChemistryWorkerRequest = ChemistryRequest & { readonly id: number };

export type ChemistryWorkerMessage =
    | { readonly type: 'ready' }
    | { readonly type: 'failed'; readonly message: string }
    | { readonly type: 'result'; readonly id: number; readonly json: string | null }
    | { readonly type: 'error'; readonly id: number; readonly message: string };