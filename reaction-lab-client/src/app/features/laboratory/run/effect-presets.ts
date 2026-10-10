import { isDevMode } from "@angular/core";
import { ReactionSummary } from "../../../data/reactions/reaction";

const EFFECT_PRESET_KEYS = [
    'white-precipitate',
    'blue-flame',
    'warm-glow',
    'color-change',
    'ionic-crystallization',
    'vigorous-bubbling',
    'furnace-heat',
    'steam-burst',
    'skittering-flame',
    'white-smoke',
    'foaming-eruption',
    'brilliant-white-flare',
    'gentle-glow',
    'white-hot-flame',
    'ember-glow',
    'foaming-gas',
    'electric-arc',
    'catalytic-glow',
    'golden-precipitate',
    'sunlight-absorption',
    'explosive-flash',
    'gentle-gas'
] as const;

export type EffectPresetKey = typeof EFFECT_PRESET_KEYS[number];

const KNOWN_KEYS: readonly string[] = EFFECT_PRESET_KEYS;

export function effectPresetKeyOf(reaction: ReactionSummary): EffectPresetKey | null {
    const key = reaction.effectPresetKey;

    if (key === null || isEffectPresetKey(key)) {
        return key;
    }

    if (isDevMode()) {
        console.warn(`Unknown effect preset "${key}" on reaction ${reaction.id}`);
    }

    return null;
}

function isEffectPresetKey(key: string): key is EffectPresetKey {
    return KNOWN_KEYS.includes(key);
}
