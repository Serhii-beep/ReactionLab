import { ReactionSummary } from "../../../data/reactions/reaction";
import { EnergyProfile, enthalpyShareAt } from "./energy-profile";
import { reactionEnergeticsOf } from "./reaction-energetics";

export interface WaterCalorimetry {
    readonly reactionMoles: number;
    readonly waterMilliliters: number;
    readonly startCelsius: number;
    readonly endCelsius: number;
    readonly profile: EnergyProfile;
}

export interface WaterReading {
    readonly celsius: number;
    readonly boiling: boolean;
    readonly freezing: boolean;
}

export const FREEZING_CELSIUS = 0;
export const BOILING_CELSIUS = 100;

const REACTION_MOLES = 0.05;
const WATER_MILLILITERS = 100;
const WATER_GRAMS_PER_MILLILITER = 1;
const WATER_JOULES_PER_GRAM_KELVIN = 4.184;
const JOULES_PER_KILOJOULE = 1000;
const STARTING_CELSIUS = 20;

export function waterCalorimetryOf(reaction: ReactionSummary, profile: EnergyProfile): WaterCalorimetry | null {
    const energetics = reactionEnergeticsOf(reaction);

    if (!energetics.inWater || energetics.activationSource !== 'heat') {
        return null;
    }

    const releasedJoules = -profile.enthalpyKilojoulesPerMole * REACTION_MOLES * JOULES_PER_KILOJOULE;
    const waterJoulesPerKelvin = WATER_MILLILITERS * WATER_GRAMS_PER_MILLILITER * WATER_JOULES_PER_GRAM_KELVIN;

    return {
        reactionMoles: REACTION_MOLES,
        waterMilliliters: WATER_MILLILITERS,
        startCelsius: STARTING_CELSIUS,
        endCelsius: STARTING_CELSIUS + releasedJoules / waterJoulesPerKelvin,
        profile
    };
}

export function waterReadingAt(calorimetry: WaterCalorimetry, seconds: number): WaterReading {
    const { startCelsius, endCelsius, profile } = calorimetry;
    const celsius = startCelsius + (endCelsius - startCelsius) * enthalpyShareAt(profile, seconds);

    return {
        celsius: Math.min(Math.max(celsius, FREEZING_CELSIUS), BOILING_CELSIUS),
        boiling: celsius >= BOILING_CELSIUS,
        freezing: celsius <= FREEZING_CELSIUS
    };
}