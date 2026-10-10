import { Color } from "three";

export type GasLookName = 'clean-flame' | 'steam' | 'precipitate';

export interface GasLook {
    readonly flameColor: Color;
    readonly flameLuminance: number;
}

export const GAS_LOOKS: Readonly<Record<GasLookName, GasLook>> = {
    'clean-flame': { flameColor: new Color(0, 0.915, 4.786), flameLuminance: 0.6 },
    'steam': { flameColor: new Color(0.56, 0.93, 2.98), flameLuminance: 0.06 },
    'precipitate': { flameColor: new Color(0, 0, 0), flameLuminance: 0 }
};