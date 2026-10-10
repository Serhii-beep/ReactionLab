import { Color } from "three";
import { PlacedAtom } from "../scene/bench-layout";
import { incandescentLuminanceOf } from "../rendering/incandescence";
import { LIT_WHITE_LUMINANCE } from "../rendering/look";

export interface LabelInk {
    readonly dark: Color;
    readonly light: Color;
    readonly ground: Color;
}

const INK_LUMINANCE = 0.35;
const GROUND_LUMINANCE = 0.5;

export function inkFor(atom: PlacedAtom, ink: LabelInk): Color {
    return litLuminanceOf(atom) > INK_LUMINANCE ? ink.dark : ink.light;
}

export function groundHaloOf(ink: LabelInk): Color {
    return luminanceOf(ink.ground) > GROUND_LUMINANCE ? ink.dark : ink.light;
}

function litLuminanceOf(atom: PlacedAtom): number {
    return luminanceOf(atom.color) + incandescentLuminanceOf(atom.temperatureKelvin) / LIT_WHITE_LUMINANCE;
}

function luminanceOf({ r, g, b }: Color): number {
    return 0.2126 * r + 0.7152 * g + 0.0722 * b;
}
