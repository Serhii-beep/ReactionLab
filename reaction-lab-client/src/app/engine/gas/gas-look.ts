import { Color } from "three";
import { LIT_WHITE_LUMINANCE } from "../rendering/look";
import { luminanceOf } from "../rendering/luminance";

export type GasLookName = 'clean-flame' | 'steam' | 'precipitate' | 'sooty-flame' | 'ionic-smoke';

export interface GasLook {
    readonly flameColor: Color;
    readonly flameLuminance: number;
    readonly particleKeepPerSecond: number;
    readonly particleSinkPerDensity: number;
}

const BLUE_FLAME = new Color(0, 0.915, 4.786);
const NO_FLAME = new Color(0, 0, 0);

export const GAS_LOOKS: Readonly<Record<GasLookName, GasLook>> = {
    'clean-flame': { flameColor: BLUE_FLAME, flameLuminance: 0.6, particleKeepPerSecond: 0.7, particleSinkPerDensity: 0 },
    'steam': { flameColor: new Color(0.56, 0.93, 2.98), flameLuminance: 0.06, particleKeepPerSecond: 0.7, particleSinkPerDensity: 0 },
    'precipitate': { flameColor: NO_FLAME, flameLuminance: 0, particleKeepPerSecond: 0.7, particleSinkPerDensity: 1.2 },
    'sooty-flame': { flameColor: BLUE_FLAME, flameLuminance: 0.35, particleKeepPerSecond: 0.75, particleSinkPerDensity: 0 },
    'ionic-smoke': { flameColor: NO_FLAME, flameLuminance: 0, particleKeepPerSecond: 0.75, particleSinkPerDensity: 0 }
};

export function flameLightOf({ flameColor, flameLuminance }: GasLook, target: Color): Color {
    return target.copy(flameColor).multiplyScalar(flameLuminance * LIT_WHITE_LUMINANCE);
}

export function emissivityOf(albedo: Color): number {
    return 1 - luminanceOf(albedo);
}
