import { BondingPicture, pairKey } from "./bonding-picture";
import { ContactRigidity } from "./dynamics-schedule";
import { MolecularSystem, Molecule, SimulatedAtom, TIME_UNITS_PER_SECOND } from "./molecular-system";

interface MoleculeGrouping {
    readonly moleculeIndexByAtom: Int32Array;
    readonly massByMolecule: Float64Array;
    readonly massShareByAtom: Float64Array;
    readonly pushByMolecule: Float64Array;
}

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
    private readonly atomReachOfPair: Float64Array;
    private readonly moleculeReachOfPair: Float64Array;
    private readonly stiffnessOfPair: Float64Array;
    private readonly nearPairIndices: Int32Array;
    private readonly positionsAtBuild: Float64Array;
    private readonly reactantGrouping: MoleculeGrouping;
    private readonly productGrouping: MoleculeGrouping;
    private nearCount = 0;

    constructor(
        private readonly system: MolecularSystem,
        reactant: BondingPicture,
        product: BondingPicture,
        reactantMolecules: readonly Molecule[],
        productMolecules: readonly Molecule[]
    ) {
        const bondedInEither = new Set([...reactant.bonds, ...product.bonds].map((bond) => pairKey(bond.first, bond.second)));
        const productRestLengthByPair = new Map(product.bonds.map((bond) => [pairKey(bond.first, bond.second), bond.restLength]));
        const { atoms } = system;
        const pairCount = (atoms.length * (atoms.length - 1)) / 2;
        let pairIndex = 0;

        this.firstAtomOfPair = new Int32Array(pairCount);
        this.secondAtomOfPair = new Int32Array(pairCount);
        this.atomReachOfPair = new Float64Array(pairCount);
        this.moleculeReachOfPair = new Float64Array(pairCount);
        this.stiffnessOfPair = new Float64Array(pairCount);
        this.nearPairIndices = new Int32Array(pairCount);
        this.positionsAtBuild = new Float64Array(system.positions.length);

        for (let first = 0; first < atoms.length; first++) {
            for (let second = first + 1; second < atoms.length; second++, pairIndex++) {
                const key = pairKey(first, second);
                const radiusSum = atoms[first].radius + atoms[second].radius;
                const reducedMass = (atoms[first].mass * atoms[second].mass) / (atoms[first].mass + atoms[second].mass);

                this.firstAtomOfPair[pairIndex] = first;
                this.secondAtomOfPair[pairIndex] = second;
                this.atomReachOfPair[pairIndex] = (bondedInEither.has(key) ? CORE_REACH_SHARE : CONTACT_REACH_SHARE) * radiusSum;
                this.moleculeReachOfPair[pairIndex] = Math.max(CONTACT_REACH_SHARE * radiusSum, productRestLengthByPair.get(key) ?? 0);
                this.stiffnessOfPair[pairIndex] = reducedMass * CORE_RATE * CORE_RATE;
            }
        }

        this.reactantGrouping = groupingOf(atoms, reactantMolecules);
        this.productGrouping = groupingOf(atoms, productMolecules);
        this.buildNearList();
    }

    accumulate(rigidity: ContactRigidity): void {
        const grouping = rigidity.grouping === 'reactants' ? this.reactantGrouping : this.productGrouping;

        if (this.movedPastMargin()) {
            this.buildNearList();
        }

        grouping.pushByMolecule.fill(0);

        for (let nearIndex = 0; nearIndex < this.nearCount; nearIndex++) {
            this.push(this.nearPairIndices[nearIndex], grouping, rigidity.wholeMoleculeShare);
        }

        this.spreadOverMolecules(grouping);
    }

    private push(pairIndex: number, grouping: MoleculeGrouping, groupingShare: number): void {
        const { positions, forces } = this.system;
        const first = this.firstAtomOfPair[pairIndex];
        const second = this.secondAtomOfPair[pairIndex];
        const firstMolecule = grouping.moleculeIndexByAtom[first];
        const secondMolecule = grouping.moleculeIndexByAtom[second];
        const wholeMoleculeShare = firstMolecule === secondMolecule ? 0 : groupingShare;
        const reach = this.atomReachOfPair[pairIndex] + wholeMoleculeShare * (this.moleculeReachOfPair[pairIndex] - this.atomReachOfPair[pairIndex]);
        const distance = Math.max(Math.sqrt(separationSquared(positions, first, second)), MIN_DISTANCE);

        if (distance >= reach) {
            return;
        }

        const overlapOverDistance = (reach - distance) / distance;
        const atomPush = (1 - wholeMoleculeShare) * this.stiffnessOfPair[pairIndex] * overlapOverDistance;

        for (let axis = 0; axis < 3; axis++) {
            forces[first * 3 + axis] -= atomPush * SEPARATION[axis];
            forces[second * 3 + axis] += atomPush * SEPARATION[axis];
        }

        if (wholeMoleculeShare > 0) {
            addMoleculePush(grouping, firstMolecule, secondMolecule, wholeMoleculeShare * overlapOverDistance);
        }
    }

    private spreadOverMolecules(grouping: MoleculeGrouping): void {
        const { forces } = this.system;

        for (let index = 0; index < grouping.moleculeIndexByAtom.length; index++) {
            const molecule = grouping.moleculeIndexByAtom[index];

            for (let axis = 0; axis < 3; axis++) {
                forces[index * 3 + axis] += grouping.massShareByAtom[index] * grouping.pushByMolecule[molecule * 3 + axis];
            }
        }
    }

    private buildNearList(): void {
        const { positions } = this.system;

        this.nearCount = 0;

        for (let pairIndex = 0; pairIndex < this.atomReachOfPair.length; pairIndex++) {
            const checkedReach = this.moleculeReachOfPair[pairIndex] + NEIGHBOR_MARGIN;

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

function groupingOf(atoms: readonly SimulatedAtom[], molecules: readonly Molecule[]): MoleculeGrouping {
    const moleculeIndexByAtom = new Int32Array(atoms.length);
    const massShareByAtom = new Float64Array(atoms.length);

    molecules.forEach((molecule, moleculeIndex) => {
        for (const index of molecule.atomIndices) {
            moleculeIndexByAtom[index] = moleculeIndex;
            massShareByAtom[index] = atoms[index].mass / molecule.mass;
        }
    });

    return {
        moleculeIndexByAtom,
        massByMolecule: Float64Array.from(molecules, (molecule) => molecule.mass),
        massShareByAtom,
        pushByMolecule: new Float64Array(molecules.length * 3)
    };
}

function addMoleculePush(grouping: MoleculeGrouping, firstMolecule: number, secondMolecule: number, scaledOverlapOverDistance: number): void {
    const firstMass = grouping.massByMolecule[firstMolecule];
    const secondMass = grouping.massByMolecule[secondMolecule];
    const push = ((firstMass * secondMass) / (firstMass + secondMass)) * CORE_RATE * CORE_RATE * scaledOverlapOverDistance;

    for (let axis = 0; axis < 3; axis++) {
        grouping.pushByMolecule[firstMolecule * 3 + axis] -= push * SEPARATION[axis];
        grouping.pushByMolecule[secondMolecule * 3 + axis] += push * SEPARATION[axis];
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