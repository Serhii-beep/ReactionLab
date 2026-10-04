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

export interface ContactRigidity {
    readonly grouping: 'reactants' | 'products';
    readonly wholeMoleculeShare: number;
}

export interface AntiBondingWeights {
    readonly reactant: number;
    readonly product: number;
}

const SWITCH_OPENING_SHARE = 0.2;
const SWITCH_CLOSING_SHARE = 0.1;
const RELEASE_SHARE_OF_SWITCH = 0.65;
const LET_GO_LEAD_SECONDS = 0.05;
const LET_GO_SHARE_OF_COLLISION = 0.3;
const RELEASE_HANDOFF_SECONDS = 0.15;

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

export function approachWeightAt(schedule: DynamicsSchedule, seconds: number): number {
    const letGoEndSeconds = schedule.collisionStartSeconds + LET_GO_SHARE_OF_COLLISION * (schedule.collisionEndSeconds - schedule.collisionStartSeconds);

    return 1 - smoothProgressBetween(schedule.collisionStartSeconds - LET_GO_LEAD_SECONDS, letGoEndSeconds, seconds);
}

export function contactRigidityAt(schedule: DynamicsSchedule, seconds: number): ContactRigidity {
    if (seconds < schedule.switchStartSeconds) {
        return { grouping: 'reactants', wholeMoleculeShare: approachWeightAt(schedule, seconds) };
    }

    const handoffStartSeconds = Math.max(schedule.releaseSeconds - RELEASE_HANDOFF_SECONDS, schedule.switchStartSeconds);

    return { grouping: 'products', wholeMoleculeShare: smoothProgressBetween(handoffStartSeconds, schedule.releaseSeconds, seconds) };
}

export function antiBondingWeightsAt(schedule: DynamicsSchedule, seconds: number): AntiBondingWeights {
    return {
        reactant: 1 - approachWeightAt(schedule, seconds),
        product: 1 - smoothProgressBetween(schedule.releaseSeconds, schedule.releaseSeconds + RELEASE_HANDOFF_SECONDS, seconds)
    };
}