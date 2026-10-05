import { Color } from "three";
import { FLAME_CEILING_KELVIN } from "../core/matter";

interface ColorMatchingLobe {
    readonly weight: number;
    readonly peakNanometers: number;
    readonly widthBelowNanometers: number;
    readonly widthAboveNanometers: number;
}

type LinearRgb = [number, number, number];

const DRAPER_POINT_KELVIN = 798;
const EMISSION_AT_FLAME_CEILING = 1.6;
const TABLE_STEP_KELVIN = 25;
const VISIBLE_NANOMETERS = { first: 380, last: 780, step: 5 };
const SECOND_RADIATION_CONSTANT_NANOMETER_KELVIN = 1.4388e7;
const COLOR_MATCHING_X: readonly ColorMatchingLobe[] = [
    { weight: 1.056, peakNanometers: 599.8, widthBelowNanometers: 37.9, widthAboveNanometers: 31.0 },
    { weight: 0.362, peakNanometers: 442.0, widthBelowNanometers: 16.0, widthAboveNanometers: 26.7 },
    { weight: -0.065, peakNanometers: 501.1, widthBelowNanometers: 20.4, widthAboveNanometers: 26.2 }
];
const COLOR_MATCHING_Y: readonly ColorMatchingLobe[] = [
    { weight: 0.821, peakNanometers: 568.8, widthBelowNanometers: 46.9, widthAboveNanometers: 40.5 },
    { weight: 0.286, peakNanometers: 530.9, widthBelowNanometers: 16.3, widthAboveNanometers: 31.1 }
];
const COLOR_MATCHING_Z: readonly ColorMatchingLobe[] = [
    { weight: 1.217, peakNanometers: 437.0, widthBelowNanometers: 11.8, widthAboveNanometers: 36.0 },
    { weight: 0.681, peakNanometers: 459.0, widthBelowNanometers: 26.0, widthAboveNanometers: 13.8 }
];
const XYZ_TO_LINEAR_SRGB = [
    [3.2406, -1.5372, -0.4986],
    [-0.9689, 1.8758, 0.0415],
    [0.0557, -0.2040, 1.0570]
] as const;
const BLACKBODY_RGB_BY_STEP = blackbodyRgbTable();

export function incandescenceOf(temperatureKelvin: number, target: Color): Color {
    if (temperatureKelvin <= DRAPER_POINT_KELVIN) {
        return target.setRGB(0, 0, 0);
    }

    const emission = EMISSION_AT_FLAME_CEILING * (1 / DRAPER_POINT_KELVIN - 1 / temperatureKelvin) / (1 / DRAPER_POINT_KELVIN - 1 / FLAME_CEILING_KELVIN);
    const step = Math.min((temperatureKelvin - DRAPER_POINT_KELVIN) / TABLE_STEP_KELVIN, BLACKBODY_RGB_BY_STEP.length - 1);
    const lower = BLACKBODY_RGB_BY_STEP[Math.floor(step)];
    const upper = BLACKBODY_RGB_BY_STEP[Math.ceil(step)];
    const amount = step - Math.floor(step);

    return target.setRGB(
        (lower[0] + (upper[0] - lower[0]) * amount) * emission,
        (lower[1] + (upper[1] - lower[1]) * amount) * emission,
        (lower[2] + (upper[2] - lower[2]) * amount) * emission
    );
}

function blackbodyRgbTable(): LinearRgb[] {
    const steps = Math.ceil((FLAME_CEILING_KELVIN - DRAPER_POINT_KELVIN) / TABLE_STEP_KELVIN) + 1;
    return Array.from({ length: steps }, (_, step) => blackbodyRgbOf(DRAPER_POINT_KELVIN + step * TABLE_STEP_KELVIN));
}

function blackbodyRgbOf(temperatureKelvin: number): LinearRgb {
    const tristimulus = [0, 0, 0];

    for (let nanometers = VISIBLE_NANOMETERS.first; nanometers <= VISIBLE_NANOMETERS.last; nanometers += VISIBLE_NANOMETERS.step) {
        const radiance = 1 / (nanometers ** 5 * (Math.exp(SECOND_RADIATION_CONSTANT_NANOMETER_KELVIN / (nanometers * temperatureKelvin)) - 1));

        tristimulus[0] += radiance * colorMatchingAt(COLOR_MATCHING_X, nanometers);
        tristimulus[1] += radiance * colorMatchingAt(COLOR_MATCHING_Y, nanometers);
        tristimulus[2] += radiance * colorMatchingAt(COLOR_MATCHING_Z, nanometers);
    }

    const linear = XYZ_TO_LINEAR_SRGB.map((row) => Math.max(row[0] * tristimulus[0] + row[1] * tristimulus[1] + row[2] * tristimulus[2], 0));
    const peak = Math.max(...linear);

    return [linear[0] / peak, linear[1] / peak, linear[2] / peak];
}

function colorMatchingAt(lobes: readonly ColorMatchingLobe[], nanometers: number): number {
    return lobes.reduce((sum, lobe) => {
        const width = nanometers < lobe.peakNanometers ? lobe.widthBelowNanometers : lobe.widthAboveNanometers;

        return sum + lobe.weight * Math.exp(-0.5 * ((nanometers - lobe.peakNanometers) / width) ** 2);
    }, 0);
}