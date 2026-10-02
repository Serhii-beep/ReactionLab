import { BondingPicture, pairKey } from "./bonding-picture";
import { MolecularSystem, TIME_UNITS_PER_SECOND } from "./molecular-system";

const CORE_REACH_SHARE = 0.6;
const CONTACT_REACH_SHARE = 0.82;
const CORE_RATE = 90 / TIME_UNITS_PER_SECOND;

const NEIGHBOR_MARGIN = 1.0;
const REBUILD_DISTANCE_SQUARED = (NEIGHBOR_MARGIN / 2) ** 2;
const MIN_DISTANCE = 1e-9;

const SEPARATION = new Float64Array(3);

export class CoreRepulsion {
    private readonly firstAtomOfPair: Int32Array;
    private readonly secondAtomOfPair: Int32Array;
    private readonly reachOfPair: Float64Array;
    private readonly stiffnessOfPair: Float64Array;
    private readonly nearPairIndices: Int32Array;
    private readonly positionsAtBuild: Float64Array;
    private nearCount = 0;

    constructor(private readonly system: MolecularSystem, reactant: BondingPicture, product: BondingPicture) {
        const bondedInEither = new Set([...reactant.bonds, ...product.bonds].map((bond) => pairKey(bond.first, bond.second)));
        const { atoms } = system;
        const pairCount = (atoms.length * (atoms.length - 1)) / 2;
        let pairIndex = 0;

        this.firstAtomOfPair = new Int32Array(pairCount);
        this.secondAtomOfPair = new Int32Array(pairCount);
        this.reachOfPair = new Float64Array(pairCount);
        this.stiffnessOfPair = new Float64Array(pairCount);
        this.nearPairIndices = new Int32Array(pairCount);
        this.positionsAtBuild = new Float64Array(system.positions.length);

        for (let first = 0; first < atoms.length; first++) {
            for (let second = first + 1; second < atoms.length; second++, pairIndex++) {
                const reachShare = bondedInEither.has(pairKey(first, second)) ? CORE_REACH_SHARE : CONTACT_REACH_SHARE;
                const reducedMass = (atoms[first].mass * atoms[second].mass) / (atoms[first].mass + atoms[second].mass);

                this.firstAtomOfPair[pairIndex] = first;
                this.secondAtomOfPair[pairIndex] = second;
                this.reachOfPair[pairIndex] = reachShare * (atoms[first].radius + atoms[second].radius);
                this.stiffnessOfPair[pairIndex] = reducedMass * CORE_RATE * CORE_RATE;
            }
        }

        this.buildNearList();
    }

    accumulate(): void {
        if (this.movedPastMargin()) {
            this.buildNearList();
        }

        for (let nearIndex = 0; nearIndex < this.nearCount; nearIndex++) {
            this.push(this.nearPairIndices[nearIndex]);
        }
    }

    private push(pairIndex: number): void {
        const { positions, forces } = this.system;
        const first = this.firstAtomOfPair[pairIndex];
        const second = this.secondAtomOfPair[pairIndex];
        const reach = this.reachOfPair[pairIndex];
        const distance = Math.max(Math.sqrt(separationSquared(positions, first, second)), MIN_DISTANCE);

        if (distance >= reach) {
            return;
        }

        const pushOverDistance = (this.stiffnessOfPair[pairIndex] * (reach - distance)) / distance;

        for (let axis = 0; axis < 3; axis++) {
            forces[first * 3 + axis] -= pushOverDistance * SEPARATION[axis];
            forces[second * 3 + axis] += pushOverDistance * SEPARATION[axis];
        }
    }

    private buildNearList(): void {
        const { positions } = this.system;

        this.nearCount = 0;

        for (let pairIndex = 0; pairIndex < this.reachOfPair.length; pairIndex++) {
            const checkedReach = this.reachOfPair[pairIndex] + NEIGHBOR_MARGIN;

            if (separationSquared(positions, this.firstAtomOfPair[pairIndex], this.secondAtomOfPair[pairIndex]) < checkedReach * checkedReach) {
                this.nearPairIndices[this.nearCount++] = pairIndex;
            }
        }

        this.positionsAtBuild.set(positions);
    }

    private movedPastMargin(): boolean {
        const { positions } = this.system;

        for (let slot = 0; slot < positions.length; slot += 3) {
            const movedX = positions[slot] - this.positionsAtBuild[slot];
            const movedY = positions[slot + 1] - this.positionsAtBuild[slot + 1];
            const movedZ = positions[slot + 2] - this.positionsAtBuild[slot + 2];

            if (movedX * movedX + movedY * movedY + movedZ * movedZ > REBUILD_DISTANCE_SQUARED) {
                return true;
            }
        }

        return false;
    }
}

function separationSquared(positions: Float64Array, first: number, second: number): number {
    let squared = 0;

    for (let axis = 0; axis < 3; axis++) {
        SEPARATION[axis] = positions[second * 3 + axis] - positions[first * 3 + axis];
        squared += SEPARATION[axis] * SEPARATION[axis];
    }

    return squared;
}