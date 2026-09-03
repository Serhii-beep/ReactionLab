import { MatterState } from "../substances/substance";

export type ReactionType =
    | 'Synthesis'
    | 'Decomposition'
    | 'SingleReplacement'
    | 'DoubleReplacement'
    | 'Combustion'
    | 'AcidBase'
    | 'Oxidation'
    | 'Reduction'
    | 'Precipitation'
    | 'Neutralization';

export type ParticipantRole = 'Reactant' | 'Product';

export type ReactantMatch = 'Complete' | 'Partial';

export interface ReactionParticipant {
    readonly substanceId: string;
    readonly formula: string;
    readonly name: string;
    readonly role: ParticipantRole;
    readonly coefficient: number;
    readonly state: MatterState | null;
}

export interface ReactionSummary {
    readonly id: string;
    readonly name: string;
    readonly type: ReactionType;
    readonly difficulty: number;
    readonly isReversible: boolean;
    readonly enthalpyKilojoulesPerMole: number | null;
    readonly isExothermic: boolean | null;
    readonly tags: readonly string[];
    readonly participants: readonly ReactionParticipant[];
}