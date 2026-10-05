import { FLAME_CEILING_KELVIN, ROOM_TEMPERATURE_KELVIN } from "../core/matter";
import { energyKeptAt } from "./dynamics/bath-cooling";
import { DynamicsSchedule } from "./dynamics/dynamics-schedule";
import { BOLTZMANN_KILOJOULES_PER_MOLE_KELVIN } from "./dynamics/langevin-bath";
import { atomIndicesByUnitId } from "./dynamics/molecular-system";
import { smoothProgressBetween } from "./easing";
import { ReactionEnergetics } from "./reaction-script";

export interface EnergyLedgerSources {
    readonly schedule: DynamicsSchedule;
    readonly energetics: ReactionEnergetics;
    readonly breakingAtomIndices: ReadonlySet<number>;
    readonly formingAtomIndices: ReadonlySet<number>;
    readonly productUnitIds: readonly string[];
}

interface ReleaseShares {
    readonly atFormingBonds: Float64Array;
    readonly acrossMolecule: Float64Array;
}

const DULONG_PETIT_KILOJOULES_PER_MOLE_KELVIN = 3 * BOLTZMANN_KILOJOULES_PER_MOLE_KELVIN;
const RELEASE_HALF_WIDTH_SECONDS = 0.06;
const SPREAD_TIME_CONSTANT_SECONDS = 0.15;

export class EnergyLedger {
    private readonly activationShares: Float64Array;
    private readonly releaseShares: ReleaseShares;

    constructor(private readonly sources: EnergyLedgerSources) {
        this.activationShares = activationSharesOf(sources);
        this.releaseShares = releaseSharesOf(sources);
    }

    temperatureKelvinAt(atomIndex: number, seconds: number): number {
        return Math.min(ROOM_TEMPERATURE_KELVIN + this.energyAt(atomIndex, seconds) / DULONG_PETIT_KILOJOULES_PER_MOLE_KELVIN, FLAME_CEILING_KELVIN);
    }

    private energyAt(atomIndex: number, seconds: number): number {
        const { schedule } = this.sources;
        const activation = this.activationShares[atomIndex] * smoothProgressBetween(schedule.collisionStartSeconds, schedule.switchStartSeconds, seconds);
        const releaseProgress = smoothProgressBetween(schedule.releaseSeconds - RELEASE_HALF_WIDTH_SECONDS, schedule.releaseSeconds + RELEASE_HALF_WIDTH_SECONDS, seconds);

        if (releaseProgress === 0) {
            return activation;
        }

        const { atFormingBonds, acrossMolecule } = this.releaseShares;
        const spread = 1 - Math.exp(-Math.max(seconds - schedule.releaseSeconds, 0) / SPREAD_TIME_CONSTANT_SECONDS);
        const carried = atFormingBonds[atomIndex] * (1 - spread) + acrossMolecule[atomIndex] * spread;

        return activation * (1 - releaseProgress) + carried * energyKeptAt(schedule, seconds) * releaseProgress;
    }
}

function thermalActivationOf(energetics: ReactionEnergetics): number {
    return energetics.activationSource === 'heat' && !energetics.inWater ? energetics.activationKilojoulesPerMole ?? 0 : 0;
}

function releasedHeatOf(energetics: ReactionEnergetics): number {
    if (energetics.inWater || energetics.enthalpyKilojoulesPerMole === null) {
        return 0;
    }

    return Math.max(thermalActivationOf(energetics) - energetics.enthalpyKilojoulesPerMole, 0);
}

function activationSharesOf({ energetics, breakingAtomIndices, formingAtomIndices, productUnitIds }: EnergyLedgerSources): Float64Array {
    const shares = new Float64Array(productUnitIds.length);
    const takers = breakingAtomIndices.size > 0 ? breakingAtomIndices : formingAtomIndices;
    const activation = thermalActivationOf(energetics);

    for (const atomIndex of takers) {
        shares[atomIndex] = activation / takers.size;
    }

    return shares;
}

function releaseSharesOf({ energetics, formingAtomIndices, productUnitIds }: EnergyLedgerSources): ReleaseShares {
    const atomCount = productUnitIds.length;
    const released = releasedHeatOf(energetics);
    const atFormingBonds = new Float64Array(atomCount);
    const acrossMolecule = new Float64Array(atomCount);

    for (const atomIndices of atomIndicesByUnitId(productUnitIds).values()) {
        const forming = atomIndices.filter((atomIndex) => formingAtomIndices.has(atomIndex));
        const share = formingAtomIndices.size > 0
            ? (released * forming.length) / formingAtomIndices.size
            : (released * atomIndices.length) / atomCount;
        const firstHolders = forming.length > 0 ? forming : atomIndices;

        for (const atomIndex of firstHolders) {
            atFormingBonds[atomIndex] = share / firstHolders.length;
        }

        for (const atomIndex of atomIndices) {
            acrossMolecule[atomIndex] = share / atomIndices.length;
        }
    }

    return { atFormingBonds, acrossMolecule };
}