import { MatterState } from "./element.model";

export interface Reaction {
    id: string;
    name: string;
    equation: string;
    equationBalanced: string | null;
    reactionType: ReactionType;
    category: string | null;
    requiredTemperature: number | null;
    requiredPressure: number | null;
    requiresCatalyst: boolean;
    catalystInfo: string | null;
    enthalpyChange: number | null;
    isExothermic: boolean | null;
    activationEnergy: number | null;
    animationType: string | null;
    effectPreset: string | null;
    animationDurationMs: number | null;
    description: string | null;
    mechanism: string | null;
    realWorldExamples: string | null;
    safetyWarnings: string | null;
    difficultyLevel: number;
    reactants: ReactionParticipant[];
    products: ReactionParticipant[];
    tags: string[];
}

export interface ReactionSummary {
    id: string;
    name: string;
    equation: string;
    reactionType: ReactionType;
    category: string | null;
    isExothermic: boolean | null;
    difficultyLevel: number;
}

export interface ReactionParticipant {
    id: string;
    elementId: string | null;
    elementSymbol: string | null;
    elementName: string | null;
    moleculeId: string | null;
    moleculeFormula: string | null;
    moleculeName: string | null;
    role: ParticipantRole;
    coefficient: number;
    state: MatterState | null;
}

export enum ReactionType {
    Synthesis,
    Decomposition,
    SingleReplacement,
    DoubleReplacement,
    Combustion,
    AcidBase,
    Oxidation,
    Reduction,
    Precipitation,
    Neutralization
}

export enum ParticipantRole {
    Reactant,
    Product
}

export interface CreateReaction {
    name: string;
    equation: string;
    equationBalanced?: string;
    reactionType: ReactionType;
    category?: string;
    requiredTemperature?: number;
    requiredPressure?: number;
    requiresCatalyst: boolean;
    catalystInfo?: string;
    enthalpyChange?: number;
    isExothermic?: boolean;
    activationEnergy?: number;
    animationType?: string;
    effectPreset?: string;
    animationDurationMs?: number;
    description?: string;
    mechanism?: string;
    realWorldExamples?: string;
    safetyWarnings?: string;
    difficultyLevel: number;
    participants?: CreateReactionParticipant[];
    tags?: string[];
}

export interface CreateReactionParticipant {
    elementId?: string;
    moleculeId?: string;
    role: ParticipantRole;
    coefficient: number;
    state?: MatterState;
}

export interface FindReactantsRequest {
    elementIds?: string[];
    moleculeIds?: string[];
}

export interface FindAvailableReactionsRequest {
    moleculeIds?: string[];
    elementIds?: string[];
    searchTerm?: string;
    pageSize?: number;
    cursor?: string;
}