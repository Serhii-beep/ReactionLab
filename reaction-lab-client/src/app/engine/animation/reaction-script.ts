import { LayoutUnit } from "../scene/bench-layout";

export interface MotionCues {
    readonly gatheredSeconds: number;
    readonly swapSeconds: number;
    readonly releaseSeconds: number;
}

export interface ReactionScript {
    readonly durationSeconds: number;
    readonly cues: MotionCues;
    readonly unitsBefore: readonly LayoutUnit[];
    readonly unitsAfter: readonly LayoutUnit[];
}