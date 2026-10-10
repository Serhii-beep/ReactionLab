import { Color } from "three";
import { ElementSummary } from "../../../data/elements/element";

export type ElementsBySymbol = ReadonlyMap<string, ElementSummary>;

const BALL_BASE = 0.22;
const BALL_SCALE = 0.32;
const FALLBACK_COVALENT_ANGSTROM = 0.75;
const FALLBACK_COLOR = '#909090';

export function ballRadiusOf(radiusAngstrom: number): number {
    return BALL_BASE + BALL_SCALE * radiusAngstrom;
}

export function covalentBallRadiusOf(element: ElementSummary | undefined): number {
    return ballRadiusOf((element?.covalentRadiusPicometers ?? FALLBACK_COVALENT_ANGSTROM * 100) / 100);
}

export function colorOf(element: ElementSummary | undefined): Color {
    return new Color(element?.displayColor ?? FALLBACK_COLOR);
}
