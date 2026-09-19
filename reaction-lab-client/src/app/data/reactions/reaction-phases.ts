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

export const DEFAULT_RUN_SECONDS = 3;

export function reactionTimeline(durationSeconds: number): ReactionTimeline {
    const approach = phase('approach', 0, 0.25 * durationSeconds);
    const collision = phase('collision', approach.endSeconds, 0.1 * durationSeconds);
    const bondsBreak = phase('bondsBreak', collision.endSeconds, 0.15 * durationSeconds);
    const transitionState = phase('transitionState', bondsBreak.endSeconds, 0.1 * durationSeconds);
    const bondsForm = phase('bondsForm', transitionState.endSeconds, 0.15 * durationSeconds);
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
    return reaction.animationDurationMilliseconds === null ? DEFAULT_RUN_SECONDS : reaction.animationDurationMilliseconds / 1000;
}

function phase(name: ReactionPhaseName, startSeconds: number, lengthSeconds: number): ReactionPhase {
    return { name, startSeconds, endSeconds: startSeconds + lengthSeconds };
}