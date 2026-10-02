import { BondKind } from "../../scene/bench-layout";
import { bondParametersFor } from "./bond-parameters";
import { SimulatedAtom } from "./molecular-system";

export interface IndexedBond {
    readonly first: number;
    readonly second: number;
    readonly kind: BondKind;
}

export interface MorseBond {
    readonly first: number;
    readonly second: number;
    readonly dissociation: number;
    readonly restLength: number;
    readonly steepness: number;
}

export interface BondBend {
    readonly outer: number;
    readonly center: number;
    readonly otherOuter: number;
    readonly restCosine: number;
    readonly linear: boolean;
    readonly stiffness: number;
}

export interface BondingPicture {
    readonly bonds: readonly MorseBond[];
    readonly bends: readonly BondBend[];
    readonly antiBonds: readonly MorseBond[];
}

const RADIANS_PER_TIME_UNIT_PER_WAVENUMBER = 0.0188365;
const BEND_STIFFNESS = 330;
const ANTI_BONDING_SHARE = 0.1;
const LINEAR_COSINE = -0.985;
const MIN_SINE_SQUARED = 0.05;
const PAIR_KEY_STRIDE = 4096;
const MIN_LENGTH_SQUARED = 1e-18;

const TO_OUTER = new Float64Array(3);
const TO_OTHER_OUTER = new Float64Array(3);

export function bondingPictureOf(bonds: readonly IndexedBond[], atoms: readonly SimulatedAtom[], rest: Float64Array): BondingPicture {
    const morseBonds = bonds.map((bond) => morseBondOf(bond, atoms, rest));

    return { bonds: morseBonds, bends: bendsOf(morseBonds, atoms.length, rest), antiBonds: [] };
}

export function withAntiBonding(picture: BondingPicture, other: BondingPicture): BondingPicture {
    const held = heldPairsOf(picture);

    return { ...picture, antiBonds: other.bonds.filter((bond) => !held.has(pairKey(bond.first, bond.second))) };
}

export function addPictureGradient(picture: BondingPicture, positions: Float64Array, gradient: Float64Array): void {
    for (const bond of picture.bonds) {
        addMorseGradient(bond, positions, gradient);
    }

    for (const bend of picture.bends) {
        addBendGradient(bend, positions, gradient);
    }

    for (const bond of picture.antiBonds) {
        addAntiBondingGradient(bond, positions, gradient);
    }
}

export function dissociationSumOf(picture: BondingPicture): number {
    return picture.bonds.reduce((sum, bond) => sum + bond.dissociation, 0);
}

export function heldPairsOf(picture: BondingPicture): Set<number> {
    const held = new Set<number>();

    for (const bond of picture.bonds) {
        held.add(pairKey(bond.first, bond.second));
    }

    for (const bend of picture.bends) {
        held.add(pairKey(bend.outer, bend.otherOuter));
    }

    return held;
}

export function pairKey(first: number, second: number): number {
    return first < second ? first * PAIR_KEY_STRIDE + second : second * PAIR_KEY_STRIDE + first;
}

function morseBondOf(bond: IndexedBond, atoms: readonly SimulatedAtom[], rest: Float64Array): MorseBond {
    const first = atoms[bond.first];
    const second = atoms[bond.second];
    const { dissociationKilojoulesPerMole, stretchWavenumber } = bondParametersFor(first.symbol, second.symbol, bond.kind);
    const reducedMass = (first.mass * second.mass) / (first.mass + second.mass);
    const omega = RADIANS_PER_TIME_UNIT_PER_WAVENUMBER * stretchWavenumber;

    return {
        first: bond.first,
        second: bond.second,
        dissociation: dissociationKilojoulesPerMole,
        restLength: distanceBetween(rest, bond.first, bond.second),
        steepness: Math.sqrt((reducedMass * omega * omega) / (2 * dissociationKilojoulesPerMole))
    };
}

function bendsOf(bonds: readonly MorseBond[], atomCount: number, rest: Float64Array): BondBend[] {
    const neighbors: number[][] = Array.from({ length: atomCount }, () => []);
    const bends: BondBend[] = [];

    for (const bond of bonds) {
        neighbors[bond.first].push(bond.second);
        neighbors[bond.second].push(bond.first);
    }

    neighbors.forEach((around, center) => {
        for (let first = 0; first < around.length; first++) {
            for (let second = first + 1; second < around.length; second++) {
                bends.push(bendOf(around[first], center, around[second], rest));
            }
        }
    });

    return bends;
}

function bendOf(outer: number, center: number, otherOuter: number, rest: Float64Array): BondBend {
    const restCosine = cosineAt(rest, outer, center, otherOuter);
    const linear = restCosine < LINEAR_COSINE;
    const stiffness = linear ? BEND_STIFFNESS : BEND_STIFFNESS / Math.max(1 - restCosine * restCosine, MIN_SINE_SQUARED);

    return { outer, center, otherOuter, restCosine, linear, stiffness };
}

function addMorseGradient(bond: MorseBond, positions: Float64Array, gradient: Float64Array): void {
    const length = distanceBetween(positions, bond.first, bond.second);
    const decay = Math.exp(-bond.steepness * (length - bond.restLength));

    addPairGradient(gradient, positions, bond, (2 * bond.dissociation * bond.steepness * decay * (1 - decay)) / length);
}

function addAntiBondingGradient(bond: MorseBond, positions: Float64Array, gradient: Float64Array): void {
    const length = distanceBetween(positions, bond.first, bond.second);
    const decay = Math.exp(-bond.steepness * (length - bond.restLength));
    const half = 0.5 * ANTI_BONDING_SHARE * bond.dissociation;

    addPairGradient(gradient, positions, bond, (-2 * bond.steepness * half * (decay * decay + decay)) / length);
}

function addBendGradient(bend: BondBend, positions: Float64Array, gradient: Float64Array): void {
    const { outer, center, otherOuter } = bend;
    let dot = 0;
    let outerSquared = MIN_LENGTH_SQUARED;
    let otherOuterSquared = MIN_LENGTH_SQUARED;

    for (let axis = 0; axis < 3; axis++) {
        TO_OUTER[axis] = positions[outer * 3 + axis] - positions[center * 3 + axis];
        TO_OTHER_OUTER[axis] = positions[otherOuter * 3 + axis] - positions[center * 3 + axis];
        dot += TO_OUTER[axis] * TO_OTHER_OUTER[axis];
        outerSquared += TO_OUTER[axis] * TO_OUTER[axis];
        otherOuterSquared += TO_OTHER_OUTER[axis] * TO_OTHER_OUTER[axis];
    }

    const lengthProduct = Math.sqrt(outerSquared * otherOuterSquared);
    const cosine = dot / lengthProduct;
    const slope = bend.linear ? bend.stiffness : bend.stiffness * (cosine - bend.restCosine);

    for (let axis = 0; axis < 3; axis++) {
        const outerComponent = slope * (TO_OTHER_OUTER[axis] / lengthProduct - (cosine * TO_OUTER[axis]) / outerSquared);
        const otherOuterComponent = slope * (TO_OUTER[axis] / lengthProduct - (cosine * TO_OTHER_OUTER[axis]) / otherOuterSquared);

        gradient[outer * 3 + axis] += outerComponent;
        gradient[otherOuter * 3 + axis] += otherOuterComponent;
        gradient[center * 3 + axis] -= outerComponent + otherOuterComponent;
    }
}

function addPairGradient(gradient: Float64Array, positions: Float64Array, bond: MorseBond, slopeOverLength: number): void {
    for (let axis = 0; axis < 3; axis++) {
        const component = slopeOverLength * (positions[bond.second * 3 + axis] - positions[bond.first * 3 + axis]);

        gradient[bond.first * 3 + axis] -= component;
        gradient[bond.second * 3 + axis] += component;
    }
}

function cosineAt(positions: Float64Array, outer: number, center: number, otherOuter: number): number {
    let dot = 0;

    for (let axis = 0; axis < 3; axis++) {
        dot += (positions[outer * 3 + axis] - positions[center * 3 + axis]) * (positions[otherOuter * 3 + axis] - positions[center * 3 + axis]);
    }

    return dot / (distanceBetween(positions, outer, center) * distanceBetween(positions, otherOuter, center));
}

function distanceBetween(positions: Float64Array, first: number, second: number): number {
    let squared = MIN_LENGTH_SQUARED;

    for (let axis = 0; axis < 3; axis++) {
        const separation = positions[second * 3 + axis] - positions[first * 3 + axis];

        squared += separation * separation;
    }

    return Math.sqrt(squared);
}