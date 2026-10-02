import { ReactionSummary } from "../../../data/reactions/reaction";
import { ReactionEnergetics } from "../../../engine/animation/reaction-script";
import { EffectPresetKey, isEffectPresetKey } from "./effect-presets";

const FLIGHT_SHARE_BY_PRESET_KEY: Readonly<Partial<Record<EffectPresetKey, number>>> = {
    'explosive-flash': 0.6,
    'blue-flame': 0.45
};
const DEFAULT_FLIGHT_SHARE = 0.35;

export function reactionEnergeticsOf(reaction: ReactionSummary): ReactionEnergetics {
    const presetKey = reaction.effectPresetKey;
    const presetFlightShare = presetKey !== null && isEffectPresetKey(presetKey) ? FLIGHT_SHARE_BY_PRESET_KEY[presetKey] : undefined;

    return {
        enthalpyKilojoulesPerMole: reaction.enthalpyKilojoulesPerMole,
        activationKilojoulesPerMole: reaction.activationEnergyKilojoulesPerMole,
        flightShare: presetFlightShare ?? DEFAULT_FLIGHT_SHARE
    };
}