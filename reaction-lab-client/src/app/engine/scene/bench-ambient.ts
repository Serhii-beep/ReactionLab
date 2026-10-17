import { Phase, ROOM_TEMPERATURE_KELVIN } from "../core/matter";
import { PlacedAtom } from "./bench-layout";

export interface BenchAmbient {
    readonly kelvin: number;
    readonly phaseBySubstanceId: ReadonlyMap<string, Phase>;
}

export const ROOM_AMBIENT: BenchAmbient = { kelvin: ROOM_TEMPERATURE_KELVIN, phaseBySubstanceId: new Map() };

export function settleInto(ambient: BenchAmbient, atoms: readonly PlacedAtom[]): void {
    for (const atom of atoms) {
        atom.temperatureKelvin = ambient.kelvin;
        atom.phase = ambient.phaseBySubstanceId.get(atom.substanceId) ?? atom.phase;
    }
}
