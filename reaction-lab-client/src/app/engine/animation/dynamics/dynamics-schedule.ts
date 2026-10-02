import { smoothProgressBetween } from "../easing";
import { PhaseSpansByName } from "../reaction-script";

export interface DynamicsSchedule {
    readonly durationSeconds: number;
    readonly approachEndSeconds: number;
    readonly collisionStartSeconds: number;
    readonly collisionEndSeconds: number;
    readonly bondsBreakStartSeconds: number;
    readonly switchStartSeconds: number;
    readonly switchEndSeconds: number;
    readonly releaseSeconds: number;
}

const SWITCH_OPENING_SHARE = 0.2;
const SWITCH_CLOSING_SHARE = 0.1;
const RELEASE_SHARE_OF_SWITCH = 0.65;

export function dynamicsScheduleOf(phases: PhaseSpansByName, durationSeconds: number): DynamicsSchedule {
    const { approach, collision, bondsBreak, transitionState, bondsForm } = phases;
    const switchStartSeconds = transitionState.start + SWITCH_OPENING_SHARE * (transitionState.end - transitionState.start);
    const switchEndSeconds = transitionState.end + SWITCH_CLOSING_SHARE * (bondsForm.end - bondsForm.start);

    return {
        durationSeconds,
        approachEndSeconds: approach.end,
        collisionStartSeconds: collision.start,
        collisionEndSeconds: collision.end,
        bondsBreakStartSeconds: bondsBreak.start,
        switchStartSeconds,
        switchEndSeconds,
        releaseSeconds: switchStartSeconds + RELEASE_SHARE_OF_SWITCH * (switchEndSeconds - switchStartSeconds)
    };
}

export function productWeightAt(schedule: DynamicsSchedule, seconds: number): number {
    return smoothProgressBetween(schedule.switchStartSeconds, schedule.switchEndSeconds, seconds);
}

export function swapSecondsOf(schedule: DynamicsSchedule): number {
    return (schedule.switchStartSeconds + schedule.switchEndSeconds) / 2;
}