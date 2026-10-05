import { ReactionParticipant, ReactionSummary } from "../../../data/reactions/reaction";
import { ActivationSource, ReactionEnergetics } from "../../../engine/animation/reaction-script";
import { EffectPresetKey, isEffectPresetKey } from "./effect-presets";

type ValueByPresetKey<Value> = Readonly<Partial<Record<EffectPresetKey, Value>>>;

const FLIGHT_SHARE_BY_PRESET_KEY: ValueByPresetKey<number> = {
    'explosive-flash': 0.6,
    'blue-flame': 0.45
};
const ACTIVATION_SOURCE_BY_PRESET_KEY: ValueByPresetKey<ActivationSource> = {
    'electric-arc': 'current',
    'sunlight-absorption': 'light'
};
const DEFAULT_FLIGHT_SHARE = 0.35;
const WATER_FORMULA = 'H2O';

export function reactionEnergeticsOf(reaction: ReactionSummary): ReactionEnergetics {
    const presetKey = reaction.effectPresetKey !== null && isEffectPresetKey(reaction.effectPresetKey) ? reaction.effectPresetKey : null;

    return {
        enthalpyKilojoulesPerMole: reaction.enthalpyKilojoulesPerMole,
        activationKilojoulesPerMole: reaction.activationEnergyKilojoulesPerMole,
        flightShare: valueForPreset(FLIGHT_SHARE_BY_PRESET_KEY, presetKey, DEFAULT_FLIGHT_SHARE),
        activationSource: valueForPreset(ACTIVATION_SOURCE_BY_PRESET_KEY, presetKey, 'heat'),
        inWater: reaction.participants.some((participant) => participant.role === 'Reactant' && isWaterBorne(participant))
    };
}

function valueForPreset<Value>(valueByPresetKey: ValueByPresetKey<Value>, presetKey: EffectPresetKey | null, fallback: Value): Value {
    return (presetKey === null ? undefined : valueByPresetKey[presetKey]) ?? fallback;
}

function isWaterBorne(participant: ReactionParticipant): boolean {
    return participant.state === 'Aqueous' || (participant.formula === WATER_FORMULA && participant.state === 'Liquid');
}