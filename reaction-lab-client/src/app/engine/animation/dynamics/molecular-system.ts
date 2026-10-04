import { Vector3 } from "three";

export interface SimulatedAtom {
    readonly symbol: string;
    readonly mass: number;
    readonly radius: number;
}

export interface Molecule {
    readonly unitId: string;
    readonly atomIndices: readonly number[];
    readonly mass: number;
}

export interface SpringPull {
    readonly omega: number;
    readonly damping: number;
    readonly weight: number;
    readonly drift: Vector3;
    readonly minPulledMass: number;
}

export const TIME_UNITS_PER_SECOND = 0.42;

export class MolecularSystem {
    readonly positions: Float64Array;
    readonly velocities: Float64Array;
    readonly forces: Float64Array;

    constructor(readonly atoms: readonly SimulatedAtom[], rest: Float64Array) {
        this.positions = Float64Array.from(rest);
        this.velocities = new Float64Array(rest.length);
        this.forces = new Float64Array(rest.length);
    }

    massWeightedMeanOf(atomIndices: readonly number[], mass: number, source: Float64Array, target: Vector3): Vector3 {
        target.set(0, 0, 0);

        for (const index of atomIndices) {
            const share = this.atoms[index].mass / mass;

            target.x += source[index * 3] * share;
            target.y += source[index * 3 + 1] * share;
            target.z += source[index * 3 + 2] * share;
        }

        return target;
    }

    accelerate(atomIndices: readonly number[], acceleration: Vector3, weight: number): void {
        for (const index of atomIndices) {
            const mass = this.atoms[index].mass * weight;

            this.forces[index * 3] += mass * acceleration.x;
            this.forces[index * 3 + 1] += mass * acceleration.y;
            this.forces[index * 3 + 2] += mass * acceleration.z;
        }
    }

    pullToward(index: number, goal: Vector3, spring: SpringPull): void {
        const mass = Math.max(this.atoms[index].mass, spring.minPulledMass) * spring.weight;
        const stiffness = spring.omega * spring.omega;
        const friction = spring.damping * spring.omega;
        const slot = index * 3;

        this.forces[slot] += mass * (stiffness * (goal.x - this.positions[slot]) - friction * (this.velocities[slot] - spring.drift.x));
        this.forces[slot + 1] += mass * (stiffness * (goal.y - this.positions[slot + 1]) - friction * (this.velocities[slot + 1] - spring.drift.y));
        this.forces[slot + 2] += mass * (stiffness * (goal.z - this.positions[slot + 2]) - friction * (this.velocities[slot + 2] - spring.drift.z));
    }

    offsetOf(index: number, center: Vector3, target: Vector3): Vector3 {
        return target.set(this.positions[index * 3] - center.x, this.positions[index * 3 + 1] - center.y, this.positions[index * 3 + 2] - center.z);
    }
}

export function moleculesOf(atoms: readonly SimulatedAtom[], unitIds: readonly string[]): Molecule[] {
    const atomIndicesByUnitId = new Map<string, number[]>();

    unitIds.forEach((unitId, index) => {
        const atomIndices = atomIndicesByUnitId.get(unitId);

        if (atomIndices) {
            atomIndices.push(index);
        } else {
            atomIndicesByUnitId.set(unitId, [index]);
        }
    });

    return [...atomIndicesByUnitId].map(([unitId, atomIndices]) => ({
        unitId,
        atomIndices,
        mass: atomIndices.reduce((sum, index) => sum + atoms[index].mass, 0)
    }));
}

export function flattened(points: readonly Vector3[]): Float64Array {
    const values = new Float64Array(points.length * 3);

    points.forEach((point, index) => {
        values[index * 3] = point.x;
        values[index * 3 + 1] = point.y;
        values[index * 3 + 2] = point.z;
    });

    return values;
}