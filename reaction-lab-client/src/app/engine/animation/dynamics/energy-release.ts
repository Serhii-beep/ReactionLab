import { Vector3 } from "three";
import { RandomSource } from "../../core/seeded-random";
import { MolecularSystem, Molecule, TIME_UNITS_PER_SECOND } from "./molecular-system";
import { BENCH_TEMPERATURE_KELVIN, BOLTZMANN_KILOJOULES_PER_MOLE_KELVIN } from "./langevin-bath";
import { ReactionEnergetics } from "../reaction-script";

export interface ReleaseBudget {
    readonly flightKilojoulesPerMole: number;
    readonly ringingKilojoulesPerMole: number;
}

export interface LaunchDraws {
    readonly uniform: RandomSource;
    readonly standardNormal: RandomSource;
}

interface Launch {
    readonly molecule: Molecule;
    readonly center: Vector3;
    readonly drift: Vector3;
    readonly velocity: Vector3;
}

export const AIR_DRAG_PER_SECOND = 1.1;

const RINGING_SHARE = 0.3;
const ENDOTHERMIC_FLIGHT_SHARE_OF_BARRIER = 0.1;
const ASSUMED_ACTIVATION_KILOJOULES_PER_MOLE = 120;
const MIN_ACTIVATION_KILOJOULES_PER_MOLE = 10;
const RINGING_FLOOR_PER_ATOM = 1.5 * BOLTZMANN_KILOJOULES_PER_MOLE_KELVIN * BENCH_TEMPERATURE_KELVIN;
const FLIGHT_SECONDS = 0.9;
const SPIN_RATE = 3.2;
const SPIN_VARIATION = { floor: 0.6, spread: 0.8 };
const MAX_RINGING_SCALE = 2;

export function releaseBudgetOf(energetics: ReactionEnergetics, enthalpyKilojoulesPerMole: number, atomCount: number): ReleaseBudget {
    const activation = Math.max(energetics.activationKilojoulesPerMole ?? ASSUMED_ACTIVATION_KILOJOULES_PER_MOLE, MIN_ACTIVATION_KILOJOULES_PER_MOLE);
    const released = Math.max(-enthalpyKilojoulesPerMole, 0);

    return {
        flightKilojoulesPerMole: released > 0 ? energetics.flightShare * released : ENDOTHERMIC_FLIGHT_SHARE_OF_BARRIER * activation,
        ringingKilojoulesPerMole: Math.max(RINGING_SHARE * released, RINGING_FLOOR_PER_ATOM * atomCount)
    };
}

export function launchProducts(system: MolecularSystem, products: readonly Molecule[], productRest: Float64Array, budget: ReleaseBudget, draws: LaunchDraws): void {
    const reachSeconds = (1 - Math.exp(-AIR_DRAG_PER_SECOND * FLIGHT_SECONDS)) / AIR_DRAG_PER_SECOND;
    const launches = products.map((molecule) => launchOf(system, molecule, productRest, reachSeconds));
    const energy = launches.reduce((sum, launch) => sum + 0.5 * launch.molecule.mass * launch.velocity.lengthSq(), 0);
    const scale = energy > budget.flightKilojoulesPerMole ? Math.sqrt(budget.flightKilojoulesPerMole / energy) : 1;
    
    for (const launch of launches) {
        launch.velocity.multiplyScalar(scale).sub(launch.drift);
        applyLaunch(system, launch, spinOf(draws));
    }
}

export function ringDown(system: MolecularSystem, products: readonly Molecule[], budget: ReleaseBudget): void {
    const drifts = products.map((molecule) => system.massWeightedMeanOf(molecule.atomIndices, molecule.mass, system.velocities, new Vector3()));
    const internal = products.reduce((sum, molecule, position) => sum + internalEnergyOf(system, molecule, drifts[position]), 0);
    const scale = internal > 1e-9 ? Math.min(Math.sqrt(budget.ringingKilojoulesPerMole / internal), MAX_RINGING_SCALE) : 1;

    products.forEach((molecule, position) => {
        const drift = drifts[position];

        for (const index of molecule.atomIndices) {
            system.velocities[index * 3] = drift.x + (system.velocities[index * 3] - drift.x) * scale;
            system.velocities[index * 3 + 1] = drift.y + (system.velocities[index * 3 + 1] - drift.y) * scale;
            system.velocities[index * 3 + 2] = drift.z + (system.velocities[index * 3 + 2] - drift.z) * scale;
        }
    });
}

function launchOf(system: MolecularSystem, molecule: Molecule, productRest: Float64Array, reachSeconds: number): Launch {
    const center = system.massWeightedMeanOf(molecule.atomIndices, molecule.mass, system.positions, new Vector3());
    const drift = system.massWeightedMeanOf(molecule.atomIndices, molecule.mass, system.velocities, new Vector3());
    const restCenter = system.massWeightedMeanOf(molecule.atomIndices, molecule.mass, productRest, new Vector3());

    return { molecule, center, drift, velocity: restCenter.sub(center).divideScalar(reachSeconds * TIME_UNITS_PER_SECOND) };
}

function spinOf(draws: LaunchDraws): Vector3 {
    const axis = new Vector3(draws.standardNormal(), draws.standardNormal(), draws.standardNormal()).normalize();

    return axis.multiplyScalar((SPIN_RATE * (SPIN_VARIATION.floor + SPIN_VARIATION.spread * draws.uniform())) / TIME_UNITS_PER_SECOND);
}

function applyLaunch(system: MolecularSystem, launch: Launch, spin: Vector3): void {
    const offset = new Vector3();
    const addedVelocity = new Vector3();

    for (const index of launch.molecule.atomIndices) {
        addedVelocity.crossVectors(spin, system.offsetOf(index, launch.center, offset)).add(launch.velocity);
        system.velocities[index * 3] += addedVelocity.x;
        system.velocities[index * 3 + 1] += addedVelocity.y;
        system.velocities[index * 3 + 2] += addedVelocity.z;
    }
}

function internalEnergyOf(system: MolecularSystem, molecule: Molecule, drift: Vector3): number {
    let energy = 0;

    for (const index of molecule.atomIndices) {
        const relativeX = system.velocities[index * 3] - drift.x;
        const relativeY = system.velocities[index * 3 + 1] - drift.y;
        const relativeZ = system.velocities[index * 3 + 2] - drift.z;

        energy += 0.5 * system.atoms[index].mass * (relativeX * relativeX + relativeY * relativeY + relativeZ * relativeZ);
    }

    return energy;
}