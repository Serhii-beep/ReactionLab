import { LayoutUnit } from "../scene/bench-layout";

export type MotionPhaseName = 'approach' | 'collision' | 'bondsBreak' | 'transitionState' | 'bondsForm' | 'separation';

export interface PhaseSpanSeconds {
    readonly start: number;
    readonly end: number;
}

export type PhaseSpansByName = Readonly<Record<MotionPhaseName, PhaseSpanSeconds>>;

export interface ReactionScript {
    readonly durationSeconds: number;
    readonly phases: PhaseSpansByName;
    readonly unitsBefore: readonly LayoutUnit[];
    readonly unitsAfter: readonly LayoutUnit[];
}