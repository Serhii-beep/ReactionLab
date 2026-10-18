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

export interface ElementEntry {
    readonly symbol: string;
    readonly mass: number;
    readonly period: number;
}

export interface SketchAtomEntry {
    readonly symbol: string;
    readonly formalCharge: number | null;
}

export type ValenceStatus = 'Complete' | 'NeedsBonds' | 'TooManyBonds' | 'Unchecked';

export interface SketchAtomDescription {
    readonly formalCharge: number;
    readonly formalChargeInferred: boolean;
    readonly lonePairs: number;
    readonly openValence: number;
    readonly status: ValenceStatus;
}

export interface RotationAxisDescription {
    readonly direction: readonly number[];
    readonly order: number;
}

export interface MirrorPlaneDescription {
    readonly normal: readonly number[];
}

export interface PointGroupDescription {
    readonly schoenflies: string;
    readonly centerAngstroms: readonly number[];
    readonly rotationAxes: readonly RotationAxisDescription[];
    readonly mirrorPlanes: readonly MirrorPlaneDescription[];
    readonly hasInversionCenter: boolean;
}

export interface SketchDescription {
    readonly atoms: readonly SketchAtomDescription[];
    readonly hill?: string;
    readonly charge: number;
    readonly gramsPerMole?: number;
    readonly positionsAngstroms?: readonly number[];
    readonly openValenceOwners?: readonly number[];
    readonly openValencePositionsAngstroms?: readonly number[];
    readonly geometryError?: string;
    readonly pointGroup?: PointGroupDescription;
}

export interface SymmetryResult {
    readonly pointGroup?: PointGroupDescription;
    readonly error?: string;
}

export type StandardState = 'Solid' | 'Liquid' | 'Gas' | 'Aqueous';

export interface PredictedParticipant {
    readonly formula: string;
    readonly coefficient: number;
    readonly phase?: StandardState;
}

export interface PredictedReaction {
    readonly reactants: readonly PredictedParticipant[];
    readonly products: readonly PredictedParticipant[];
    readonly rule: string;
    readonly confidence: number;
    readonly minimumKelvin?: number;
    readonly enthalpyKilojoulesPerMole?: number;
    readonly activationEnergyKilojoulesPerMole?: number;
}

export interface PredictionResult {
    readonly reactions?: readonly PredictedReaction[];
    readonly error?: string;
    readonly invalidFormula?: string;
}

export interface ConditionsParticipantEntry {
    readonly formula: string;
    readonly phase: StandardState;
    readonly stoichiometricNumber: number;
    readonly moles: number;
}

export interface ConditionsReactionEntry {
    readonly participants: readonly ConditionsParticipantEntry[];
    readonly activationEnergyKilojoulesPerMole?: number;
}

export interface ConditionsRequest {
    readonly kelvin: number;
    readonly kilopascals: number;
    readonly reactions: readonly ConditionsReactionEntry[];
    readonly includeCurves?: boolean;
}

export interface ReactionKinetics {
    readonly log10RateFactor: number;
    readonly log10ShareOverBarrier: number;
    readonly log10CollisionFactor?: number;
}

export interface ReactionEquilibrium {
    readonly enthalpyKilojoulesPerMole: number;
    readonly entropyJoulesPerMoleKelvin: number;
    readonly gibbsKilojoulesPerMole: number;
    readonly log10EquilibriumConstant: number;
    readonly crossoverKelvin?: number;
    readonly phases: readonly StandardState[];
    readonly conversion: number;
    readonly molesAtEquilibrium: readonly number[];
    readonly halfConversionKelvin?: number;
}

export interface ConditionsCurve {
    readonly kelvins: readonly number[];
    readonly conversions?: readonly (number | null)[];
    readonly conversionsAtTenfoldPressure?: readonly (number | null)[];
    readonly conversionsAtTenthPressure?: readonly (number | null)[];
    readonly log10SharesOverBarrier?: readonly number[];
}

export interface ReactionConditions {
    readonly kinetics?: ReactionKinetics;
    readonly equilibrium?: ReactionEquilibrium;
    readonly curve?: ConditionsCurve;
    readonly speciesWithoutEntropy: readonly string[];
}

export interface ConditionsResult {
    readonly thermalEnergyKilojoulesPerMole: number;
    readonly reactions: readonly ReactionConditions[];
}

export interface StructureKeyResult {
    readonly key: string;
}

export type ChemistryRequest =
    | { readonly operation: 'loadReferenceData'; readonly elementsJson: string; readonly referenceJson: string }
    | { readonly operation: 'parseFormula'; readonly formula: string }
    | { readonly operation: 'molarMassOf'; readonly formula: string }
    | { readonly operation: 'balance'; readonly requestJson: string }
    | { readonly operation: 'assemble'; readonly requestJson: string }
    | { readonly operation: 'describeSketch'; readonly requestJson: string }
    | { readonly operation: 'structureKey'; readonly requestJson: string }
    | { readonly operation: 'detectSymmetry'; readonly requestJson: string }
    | { readonly operation: 'predict'; readonly requestJson: string }
    | { readonly operation: 'describeConditions'; readonly requestJson: string };

export interface ChemistryResultByOperation {
    readonly parseFormula: FormulaResult;
    readonly molarMassOf: MolarMassResult;
    readonly balance: BalanceResult;
    readonly assemble: AssemblyResult;
    readonly describeSketch: SketchDescription;
    readonly structureKey: StructureKeyResult;
    readonly detectSymmetry: SymmetryResult;
    readonly predict: PredictionResult;
    readonly describeConditions: ConditionsResult;
}

export type ChemistryWorkerRequest = ChemistryRequest & { readonly id: number };

export type ChemistryWorkerMessage =
    | { readonly type: 'ready' }
    | { readonly type: 'failed'; readonly message: string }
    | { readonly type: 'result'; readonly id: number; readonly json: string | null }
    | { readonly type: 'error'; readonly id: number; readonly message: string };