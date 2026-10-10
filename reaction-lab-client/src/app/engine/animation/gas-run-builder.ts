import { GasCloud, GasRun } from "../gas/gas-run";
import { AtomPair } from "./atom-pairing";
import { DynamicsSources } from "./dynamics-input-builder";
import { shownEnthalpyOf } from "./energy-ledger";
import { ReactionTrace } from "./reaction-trace";

const WATER_SYMBOLS = ['H', 'H', 'O'];

export function buildGasRun(sources: DynamicsSources, trace: ReactionTrace): GasRun | null {
    const { script, atomPairs } = sources;

    if (script.gasLook === null) {
        return null;
    }

    return {
        look: script.gasLook,
        trace,
        waterOxygens: waterOxygensOf(atomPairs),
        releasesHeat: (shownEnthalpyOf(script.energetics) ?? 0) < 0,
        cloud: cloudOf(sources)
    };
}

function cloudOf({ script, atomPairs }: DynamicsSources): GasCloud | null {
    const { precipitate } = script;

    if (precipitate === null) {
        return null;
    }

    return {
        atomIndices: atomPairs.flatMap(({ product }, atomIndex) => product.substanceId === precipitate.substanceId ? [atomIndex] : []),
        albedo: precipitate.cloudColor
    };
}

function waterOxygensOf(atomPairs: readonly AtomPair[]): number[] {
    const symbolsByUnitId = new Map<string, string[]>();

    for (const { product } of atomPairs) {
        symbolsByUnitId.set(product.unitId, [...(symbolsByUnitId.get(product.unitId) ?? []), product.symbol]);
    }

    return atomPairs.flatMap(({ product }, atomIndex) => product.symbol === 'O' && isWater(symbolsByUnitId.get(product.unitId) ?? []) ? [atomIndex] : []);
}

function isWater(symbols: readonly string[]): boolean {
    return [...symbols].sort().join() === WATER_SYMBOLS.join();
}
