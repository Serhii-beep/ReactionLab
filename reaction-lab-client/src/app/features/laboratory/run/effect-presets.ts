import { isDevMode } from "@angular/core";
import { ReactionSummary } from "../../../data/reactions/reaction";
import { ReactionTimeline } from "../../../data/reactions/reaction-phases";
import { EmissionPlan } from "../../../engine/particles/emission-plan";

export type EffectPresetKey =
    | 'white-precipitate'
    | 'blue-flame'
    | 'warm-glow'
    | 'color-change'
    | 'ionic-crystallization'
    | 'vigorous-bubbling'
    | 'furnace-heat'
    | 'steam-burst'
    | 'skittering-flame'
    | 'white-smoke'
    | 'foaming-eruption'
    | 'brilliant-white-flare'
    | 'gentle-glow'
    | 'white-hot-flame'
    | 'ember-glow'
    | 'foaming-gas'
    | 'electric-arc'
    | 'catalytic-glow'
    | 'golden-precipitate'
    | 'sunlight-absorption'
    | 'explosive-flash'
    | 'gentle-gas';

interface Tint {
    readonly colorStart: number;
    readonly colorEnd: number;
}

interface EmissionOptions {
    readonly particleCount?: number;
    readonly intensity?: number;
    readonly opacity?: number;
    readonly magnitude?: number;
}

type EmissionRecipe = (timeline: ReactionTimeline) => EmissionPlan[];

const ORANGE_FLAME: Tint = { colorStart: 0xffb347, colorEnd: 0xff3b0a };
const BLUE_FLAME: Tint = { colorStart: 0x8ad4ff, colorEnd: 0x1f4dff };
const WHITE_FLAME: Tint = { colorStart: 0xfff6e0, colorEnd: 0xffc36b };
const EMBER: Tint = { colorStart: 0xff9a3c, colorEnd: 0x7a1f00 };
const WARM_GLOW: Tint = { colorStart: 0xffd08a, colorEnd: 0xff9a3c };
const SPARK: Tint = { colorStart: 0xffe9a8, colorEnd: 0xff5a1f };
const WHITE_SPARK: Tint = { colorStart: 0xffffff, colorEnd: 0xdfe8ff };
const ARC: Tint = { colorStart: 0xd8f0ff, colorEnd: 0x5aa0ff };
const SMOKE: Tint = { colorStart: 0x9aa1a8, colorEnd: 0x5c626a };
const STEAM: Tint = { colorStart: 0xffffff, colorEnd: 0xdfe6ee };
const WHITE_SOLID: Tint = { colorStart: 0xffffff, colorEnd: 0xf2f4f7 };
const GOLD_SOLID: Tint = { colorStart: 0xf3c14b, colorEnd: 0xd89a1e };
const CRYSTAL: Tint = { colorStart: 0xe8ecf4, colorEnd: 0xc9d2e0 };

const SPARK_LIFE_SECONDS = 0.7;
const FLAME_LIFE_SECONDS = 0.8;
const SMOKE_LIFE_SECONDS = 2.4;
const STEAM_LIFE_SECONDS = 2;
const PRECIPITATE_TAIL_SECONDS = 1.5;

const WARM_GLOW_RECIPE: EmissionRecipe = (timeline) => [flame(timeline, WARM_GLOW, { intensity: 0.8, opacity: 0.25, particleCount: 40, magnitude: 1.3 })];

const RECIPES: Readonly<Record<EffectPresetKey, EmissionRecipe | null>> = {
    'white-precipitate': (timeline) => [precipitate(timeline, WHITE_SOLID)],
    'blue-flame': (timeline) => [flame(timeline, BLUE_FLAME, { intensity: 1.6 }), sparks(timeline, SPARK), smoke(timeline, SMOKE)],
    'warm-glow': WARM_GLOW_RECIPE,
    'color-change': null,
    'ionic-crystallization': (timeline) => [precipitate(timeline, CRYSTAL, { particleCount: 60, magnitude: 1.6 })],
    'vigorous-bubbling': null,
    'furnace-heat': (timeline) => [flame(timeline, ORANGE_FLAME, { intensity: 1.6, magnitude: 1.3 }), smoke(timeline, SMOKE, { opacity: 0.14, particleCount: 50 })],
    'steam-burst': (timeline) => [steamBurst(timeline, STEAM)],
    'skittering-flame': (timeline) => [flame(timeline, ORANGE_FLAME, { particleCount: 60, magnitude: 0.7 }), sparks(timeline, SPARK, { particleCount: 120 })],
    'white-smoke': (timeline) => [smoke(timeline, STEAM, { opacity: 0.16, particleCount: 60 })],
    'foaming-eruption': null,
    'brilliant-white-flare': (timeline) => [sparks(timeline, WHITE_SPARK, { particleCount: 260, magnitude: 1.3 }), flame(timeline, WHITE_FLAME, { intensity: 2.2, particleCount: 60 })],
    'gentle-glow': WARM_GLOW_RECIPE,
    'white-hot-flame': (timeline) => [flame(timeline, WHITE_FLAME, { intensity: 2 }), sparks(timeline, WHITE_SPARK, { particleCount: 200 }), smoke(timeline, SMOKE)],
    'ember-glow': (timeline) => [flame(timeline, EMBER, { intensity: 1.1, particleCount: 60 }), smoke(timeline, SMOKE, { opacity: 0.1 })],
    'foaming-gas': null,
    'electric-arc': (timeline) => [sparks(timeline, ARC, { particleCount: 200, magnitude: 0.8 })],
    'catalytic-glow': (timeline) => [flame(timeline, WARM_GLOW, { intensity: 1, opacity: 0.3, particleCount: 50, magnitude: 1.2 })],
    'golden-precipitate': (timeline) => [precipitate(timeline, GOLD_SOLID)],
    'sunlight-absorption': null,
    'explosive-flash': (timeline) => [sparks(timeline, SPARK, { particleCount: 260, magnitude: 1.3 }), flame(timeline, ORANGE_FLAME, { particleCount: 70 }), smoke(timeline, SMOKE, { opacity: 0.14, particleCount: 50 })],
    'gentle-gas': null
};

const KNOWN_KEYS: readonly string[] = Object.keys(RECIPES);

export function emissionPlansFor(reaction: ReactionSummary, timeline: ReactionTimeline): EmissionPlan[] {
    const key = reaction.effectPresetKey;

    if (key === null) {
        return [];
    }

    if (!isEffectPresetKey(key)) {
        if (isDevMode()) {
            console.warn(`Unknown effect preset "${key}" on reaction ${reaction.id}`);
        }

        return [];
    }

    const recipe = RECIPES[key];

    return recipe === null ? [] : recipe(timeline);
}

function isEffectPresetKey(key: string): key is EffectPresetKey {
    return KNOWN_KEYS.includes(key);
}

function flame(timeline: ReactionTimeline, tint: Tint, options: EmissionOptions = {}): EmissionPlan {
    const { collision, bondsForm } = timeline.byName;

    return {
        kind: 'flame',
        anchor: 'meeting',
        startSeconds: collision.startSeconds,
        windowSeconds: bondsForm.endSeconds - collision.startSeconds,
        lifeSeconds: FLAME_LIFE_SECONDS,
        particleCount: options.particleCount ?? 90,
        spreadAngstrom: 0.5,
        magnitude: options.magnitude ?? 1,
        ...tint,
        intensity: options.intensity ?? 1.4,
        opacity: options.opacity ?? 0.35
    };
}

function sparks(timeline: ReactionTimeline, tint: Tint, options: EmissionOptions = {}): EmissionPlan {
    const { transitionState } = timeline.byName;

    return {
        kind: 'sparks',
        anchor: 'meeting',
        startSeconds: transitionState.startSeconds,
        windowSeconds: transitionState.endSeconds - transitionState.startSeconds,
        lifeSeconds: SPARK_LIFE_SECONDS,
        particleCount: options.particleCount ?? 160,
        spreadAngstrom: 0.3,
        magnitude: options.magnitude ?? 1,
        ...tint,
        intensity: options.intensity ?? 6,
        opacity: options.opacity ?? 1
    };
}

function smoke(timeline: ReactionTimeline, tint: Tint, options: EmissionOptions = {}): EmissionPlan {
    const { transitionState, separation } = timeline.byName;

    return {
        kind: 'smoke',
        anchor: 'meeting',
        startSeconds: transitionState.endSeconds,
        windowSeconds: separation.endSeconds - separation.startSeconds,
        lifeSeconds: SMOKE_LIFE_SECONDS,
        particleCount: options.particleCount ?? 40,
        spreadAngstrom: 0.5,
        magnitude: options.magnitude ?? 1,
        ...tint,
        intensity: options.intensity ?? 1,
        opacity: options.opacity ?? 0.12
    };
}

function steamBurst(timeline: ReactionTimeline, tint: Tint): EmissionPlan {
    const { transitionState } = timeline.byName;

    return {
        kind: 'smoke',
        anchor: 'meeting',
        startSeconds: transitionState.startSeconds,
        windowSeconds: transitionState.endSeconds - transitionState.startSeconds,
        lifeSeconds: STEAM_LIFE_SECONDS,
        particleCount: 60,
        spreadAngstrom: 0.5,
        magnitude: 1.2,
        ...tint,
        intensity: 1,
        opacity: 0.16
    };
}

function precipitate(timeline: ReactionTimeline, tint: Tint, options: EmissionOptions = {}): EmissionPlan {
    const { bondsForm } = timeline.byName;

    return {
        kind: 'precipitate',
        anchor: 'products',
        startSeconds: bondsForm.startSeconds,
        windowSeconds: bondsForm.endSeconds - bondsForm.startSeconds,
        lifeSeconds: timeline.durationSeconds - bondsForm.startSeconds + PRECIPITATE_TAIL_SECONDS,
        particleCount: options.particleCount ?? 140,
        spreadAngstrom: 0.9,
        magnitude: options.magnitude ?? 1,
        ...tint,
        intensity: options.intensity ?? 1,
        opacity: options.opacity ?? 0.95
    };
}