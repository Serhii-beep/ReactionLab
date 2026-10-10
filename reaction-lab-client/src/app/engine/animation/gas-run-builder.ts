import { GasParticles, GasRun } from "../gas/gas-run";
import { AtomPair } from "./atom-pairing";
import { DynamicsSources } from "./dynamics-input-builder";
import { shownEnthalpyOf } from "./energy-ledger";
import { ParticlePlan } from "./reaction-script";
import { ReactionTrace } from "./reaction-trace";

const WATER_SYMBOLS = ['H', 'H', 'O'];

export function buildGasRun({ script, atomPairs }: DynamicsSources, trace: ReactionTrace): GasRun | null {
    const { gas } = script;

    if (gas === null) {
        return null;
    }

    const releasesHeat = (shownEnthalpyOf(script.energetics) ?? 0) < 0;

    return {
        look: gas.look,
        trace,
        waterOxygens: releasesHeat || gas.look === 'steam' ? waterOxygensOf(atomPairs) : [],
        releasesHeat,
        particles: gas.particles === null ? null : particlesOf(gas.particles, atomPairs)
    };
}

function particlesOf({ substanceIds, seedSymbol, albedo, loading }: ParticlePlan, atomPairs: readonly AtomPair[]): GasParticles {
    const seededIds = new Set(substanceIds);

    return {
        atomIndices: atomPairs.flatMap(({ product }, atomIndex) => seededIds.has(product.substanceId) && (seedSymbol === null || product.symbol === seedSymbol) ? [atomIndex] : []),
        albedo,
        loading
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
