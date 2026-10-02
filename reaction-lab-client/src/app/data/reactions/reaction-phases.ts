import { ReactionSummary } from "./reaction";

export type ReactionPhaseName = 'approach' | 'collision' | 'bondsBreak' | 'transitionState' | 'bondsForm' | 'separation';

export interface ReactionPhase {
    readonly name: ReactionPhaseName;
    readonly startSeconds: number;
    readonly endSeconds: number;
}

export interface ReactionTimeline {
    readonly durationSeconds: number;
    readonly phases: readonly ReactionPhase[];
    readonly byName: Readonly<Record<ReactionPhaseName, ReactionPhase>>;
}

export const DEFAULT_RUN_SECONDS = 5;

const CATALOG_PACE_FACTOR = 5 / 3;
const INNER_SHARE_WITHOUT_BARRIER = 0.35;
const INNER_SHARE_PER_BARRIER = 0.15;
const INNER_WEIGHTS = { collision: 8 / 39, bondsBreak: 10 / 39, transitionState: 9 / 39, bondsForm: 12 / 39 };
const APPROACH_SHARE_OF_OUTER = 28 / 61;
const BARRIER_REFERENCE_KILOJOULES_PER_MOLE = 250;

export function reactionTimeline(durationSeconds: number, activationBarrier = 0): ReactionTimeline {
    const innerSeconds = (INNER_SHARE_WITHOUT_BARRIER + INNER_SHARE_PER_BARRIER * activationBarrier) * durationSeconds;
    const approach = phase('approach', 0, (durationSeconds - innerSeconds) * APPROACH_SHARE_OF_OUTER);
    const collision = phase('collision', approach.endSeconds, innerSeconds * INNER_WEIGHTS.collision);
    const bondsBreak = phase('bondsBreak', collision.endSeconds, innerSeconds * INNER_WEIGHTS.bondsBreak);
    const transitionState = phase('transitionState', bondsBreak.endSeconds, innerSeconds * INNER_WEIGHTS.transitionState);
    const bondsForm = phase('bondsForm', transitionState.endSeconds, innerSeconds * INNER_WEIGHTS.bondsForm);
    const separation = phase('separation', bondsForm.endSeconds, durationSeconds - bondsForm.endSeconds);

    return {
        durationSeconds,
        phases: [approach, collision, bondsBreak, transitionState, bondsForm, separation],
        byName: { approach, collision, bondsBreak, transitionState, bondsForm, separation }
    };
}

export function phaseAt(timeline: ReactionTimeline, seconds: number): ReactionPhase {
    return timeline.phases.find((phase) => seconds < phase.endSeconds) ?? timeline.byName.separation;
}

export function runSecondsOf(reaction: ReactionSummary): number {
    return reaction.animationDurationMilliseconds === null ? DEFAULT_RUN_SECONDS : (reaction.animationDurationMilliseconds / 1000) * CATALOG_PACE_FACTOR;
}

export function activationBarrierOf(reaction: ReactionSummary): number {
    const energy = reaction.activationEnergyKilojoulesPerMole ?? 0;

    return Math.min(Math.max(energy / BARRIER_REFERENCE_KILOJOULES_PER_MOLE, 0), 1);
}

function phase(name: ReactionPhaseName, startSeconds: number, lengthSeconds: number): ReactionPhase {
    return { name, startSeconds, endSeconds: startSeconds + lengthSeconds };
}