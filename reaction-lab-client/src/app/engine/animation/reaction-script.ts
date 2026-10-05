import { EmissionPlan } from "../particles/emission-plan";
import { LayoutUnit } from "../scene/bench-layout";

export type MotionPhaseName = 'approach' | 'collision' | 'bondsBreak' | 'transitionState' | 'bondsForm' | 'separation';

export interface PhaseSpanSeconds {
    readonly start: number;
    readonly end: number;
}

export type PhaseSpansByName = Readonly<Record<MotionPhaseName, PhaseSpanSeconds>>;

export type ActivationSource = 'heat' | 'current' | 'light';

export interface ReactionEnergetics {
    readonly enthalpyKilojoulesPerMole: number | null;
    readonly activationKilojoulesPerMole: number | null;
    readonly flightShare: number;
    readonly activationSource: ActivationSource;
    readonly inWater: boolean;
}

export interface ReactionScript {
    readonly durationSeconds: number;
    readonly phases: PhaseSpansByName;
    readonly energetics: ReactionEnergetics;
    readonly massBySymbol: ReadonlyMap<string, number>;
    readonly randomSeed: string;
    readonly emissions: readonly EmissionPlan[];
    readonly unitsBefore: readonly LayoutUnit[];
    readonly unitsAfter: readonly LayoutUnit[];
}