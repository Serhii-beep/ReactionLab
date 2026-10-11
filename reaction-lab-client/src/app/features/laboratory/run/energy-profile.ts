import { ReactionSummary } from "../../../data/reactions/reaction";
import { ReactionTimeline } from "../../../data/reactions/reaction-phases";
import { dynamicsScheduleOf, swapSecondsOf } from "../../../engine/animation/dynamics/dynamics-schedule";
import { progressBetween, smoothProgressBetween } from "../../../engine/animation/easing";
import { ActivationSource } from "../../../engine/animation/reaction-script";
import { reactionEnergeticsOf } from "./reaction-energetics";
import { phaseSpansOf } from "./reaction-script-builder";

export interface EnergyProfile {
    readonly enthalpyKilojoulesPerMole: number;
    readonly activationKilojoulesPerMole: number | null;
    readonly riseSeconds: number;
    readonly peakSeconds: number;
    readonly settleSeconds: number;
}

export type EnergyTone = 'exothermic' | 'endothermic' | 'unmeasured';

export interface EnergyVerdict {
    readonly messageKey: string;
    readonly tone: EnergyTone;
    readonly kilojoulesPerMole: number | null;
    readonly heatDriven: boolean;
}

const ABSORBS_KEY_BY_SOURCE: Readonly<Record<ActivationSource, string>> = {
    heat: 'absorbs',
    current: 'absorbsFromCurrent',
    light: 'absorbsFromLight'
};

export function energyProfileOf(reaction: ReactionSummary, timeline: ReactionTimeline): EnergyProfile | null {
    const enthalpy = reaction.enthalpyKilojoulesPerMole;

    if (enthalpy === null) {
        return null;
    }

    const schedule = dynamicsScheduleOf(phaseSpansOf(timeline), timeline.durationSeconds);

    return {
        enthalpyKilojoulesPerMole: enthalpy,
        activationKilojoulesPerMole: reaction.activationEnergyKilojoulesPerMole,
        riseSeconds: timeline.byName.collision.startSeconds,
        peakSeconds: swapSecondsOf(schedule),
        settleSeconds: timeline.byName.separation.startSeconds
    };
}

export function energyAt(profile: EnergyProfile, seconds: number): number {
    const { enthalpyKilojoulesPerMole: enthalpy, activationKilojoulesPerMole: activation, riseSeconds, peakSeconds, settleSeconds } = profile;

    if (activation === null) {
        return enthalpy * progressBetween(riseSeconds, settleSeconds, seconds);
    }

    if (seconds <= peakSeconds) {
        return activation * smoothProgressBetween(riseSeconds, peakSeconds, seconds);
    }

    return activation + (enthalpy - activation) * smoothProgressBetween(peakSeconds, settleSeconds, seconds);
}

export function enthalpyShareAt(profile: EnergyProfile, seconds: number): number {
    const enthalpy = profile.enthalpyKilojoulesPerMole;

    return enthalpy === 0 ? 0 : Math.min(Math.max(energyAt(profile, seconds) / enthalpy, 0), 1);
}

export function energyVerdictOf(reaction: ReactionSummary): EnergyVerdict {
    const enthalpy = reaction.enthalpyKilojoulesPerMole;

    if (enthalpy === null) {
        return { messageKey: 'noEnthalpy', tone: 'unmeasured', kilojoulesPerMole: null, heatDriven: true };
    }

    const source = reactionEnergeticsOf(reaction).activationSource;
    const heatDriven = source === 'heat';

    if (enthalpy < 0) {
        return { messageKey: 'releases', tone: 'exothermic', kilojoulesPerMole: -enthalpy, heatDriven };
    }

    return { messageKey: ABSORBS_KEY_BY_SOURCE[source], tone: 'endothermic', kilojoulesPerMole: enthalpy, heatDriven };
}