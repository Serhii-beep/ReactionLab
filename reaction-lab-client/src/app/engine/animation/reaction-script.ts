import { Color } from "three";
import { LayoutUnit } from "../scene/bench-layout";
import { GasLookName } from "../gas/gas-look";

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

export interface ParticlePlan {
    readonly substanceIds: readonly string[];
    readonly seedSymbol: string | null;
    readonly albedo: Color;
    readonly loading: number;
}

export interface GasPlan {
    readonly look: GasLookName;
    readonly particles: ParticlePlan | null;
}

export interface PrecipitatePlan {
    readonly substanceId: string;
    readonly standIns: readonly LayoutUnit[];
}

export interface ReactionScript {
    readonly durationSeconds: number;
    readonly phases: PhaseSpansByName;
    readonly energetics: ReactionEnergetics;
    readonly massBySymbol: ReadonlyMap<string, number>;
    readonly randomSeed: string;
    readonly gas: GasPlan | null;
    readonly precipitate: PrecipitatePlan | null;
    readonly unitsBefore: readonly LayoutUnit[];
    readonly unitsAfter: readonly LayoutUnit[];
}
