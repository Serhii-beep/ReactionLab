import { ReactionSummary } from "../../../data/reactions/reaction";
import { EnergyProfile, enthalpyShareAt } from "./energy-profile";
import { reactionEnergeticsOf } from "./reaction-energetics";

export interface WaterCalorimetry {
    readonly reactionMoles: number;
    readonly waterMilliliters: number;
    readonly startCelsius: number;
    readonly endCelsius: number;
    readonly boilingCelsius: number;
    readonly profile: EnergyProfile;
}

export interface WaterReading {
    readonly celsius: number;
    readonly boiling: boolean;
    readonly freezing: boolean;
}

export const FREEZING_CELSIUS = 0;

const REACTION_MOLES = 0.05;
const WATER_MILLILITERS = 100;
const WATER_GRAMS_PER_MILLILITER = 1;
const WATER_JOULES_PER_GRAM_KELVIN = 4.184;
const JOULES_PER_KILOJOULE = 1000;

export function waterCalorimetryOf(reaction: ReactionSummary, profile: EnergyProfile, startCelsius: number, boilingCelsius: number): WaterCalorimetry | null {
    const energetics = reactionEnergeticsOf(reaction);

    if (!energetics.inWater || energetics.activationSource !== 'heat') {
        return null;
    }

    const releasedJoules = -profile.enthalpyKilojoulesPerMole * REACTION_MOLES * JOULES_PER_KILOJOULE;
    const waterJoulesPerKelvin = WATER_MILLILITERS * WATER_GRAMS_PER_MILLILITER * WATER_JOULES_PER_GRAM_KELVIN;

    return {
        reactionMoles: REACTION_MOLES,
        waterMilliliters: WATER_MILLILITERS,
        startCelsius,
        endCelsius: startCelsius + releasedJoules / waterJoulesPerKelvin,
        boilingCelsius,
        profile
    };
}

export function waterReadingAt(calorimetry: WaterCalorimetry, seconds: number): WaterReading {
    const { startCelsius, endCelsius, boilingCelsius, profile } = calorimetry;
    const celsius = startCelsius + (endCelsius - startCelsius) * enthalpyShareAt(profile, seconds);

    return {
        celsius: Math.min(Math.max(celsius, FREEZING_CELSIUS), boilingCelsius),
        boiling: celsius >= boilingCelsius,
        freezing: celsius < FREEZING_CELSIUS
    };
}