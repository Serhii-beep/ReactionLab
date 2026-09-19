import { PlacedAtom, PlacedBond } from "../scene/bench-layout";
import { AtomPair } from "./atom-pairing";
import { BondByAtomPair, bondSurvives } from "./bond-continuity";
import { PosedPoints, posedPositionOf } from "./unit-gathering";

type BondsByAtom = ReadonlyMap<PlacedAtom, readonly PlacedBond[]>;

interface Refinement {
    readonly productByReactant: Map<PlacedAtom, PlacedAtom>;
    readonly reactantBondsByAtom: BondsByAtom;
    readonly productBondByEnds: BondByAtomPair;
    readonly gathered: PosedPoints;
}

interface PairingScore {
    readonly survivingBonds: number;
    readonly travelSquared: number;
}

const MAX_PASSES = 6;
const TRAVEL_EPSILON = 1e-6;

export function refinePairing(
    atomPairs: readonly AtomPair[],
    reactantBonds: readonly PlacedBond[],
    productBondByEnds: BondByAtomPair,
    gathered: PosedPoints
): AtomPair[] {
    const refinement: Refinement = {
        productByReactant: new Map(atomPairs.map((pair) => [pair.reactant, pair.product])),
        reactantBondsByAtom: indexBondsByAtom(reactantBonds),
        productBondByEnds,
        gathered
    };
    const reactants = atomPairs.map((pair) => pair.reactant);
    let pass = 0;

    while (pass < MAX_PASSES && swapPairsOnce(reactants, refinement)) {
        pass++;
    }

    return reactants.map((reactant) => ({ reactant, product: partnerOf(refinement, reactant) }));
}

function swapPairsOnce(reactants: readonly PlacedAtom[], refinement: Refinement): boolean {
    let improved = false;

    for (let first = 0; first < reactants.length; first++) {
        for (let second = first + 1; second < reactants.length; second++) {
            if (reactants[first].symbol === reactants[second].symbol && swapIfBetter(reactants[first], reactants[second], refinement)) {
                improved = true;
            }
        }
    }

    return improved;
}

function swapIfBetter(first: PlacedAtom, second: PlacedAtom, refinement: Refinement): boolean {
    const before = scoreAround(first, second, refinement);

    swapPartners(first, second, refinement.productByReactant);

    const after = scoreAround(first, second, refinement);

    if (after.survivingBonds > before.survivingBonds
        || (after.survivingBonds === before.survivingBonds && after.travelSquared < before.travelSquared - TRAVEL_EPSILON)) {
        return true;
    }

    swapPartners(first, second, refinement.productByReactant);

    return false;
}

function scoreAround(first: PlacedAtom, second: PlacedAtom, refinement: Refinement): PairingScore {
    const bonds = new Set([...(refinement.reactantBondsByAtom.get(first) ?? []), ...(refinement.reactantBondsByAtom.get(second) ?? [])]);
    let survivingBonds = 0;

    for (const bond of bonds) {
        if (bondSurvives(bond, refinement.productByReactant, refinement.productBondByEnds)) {
            survivingBonds++;
        }
    }

    return { survivingBonds, travelSquared: travelSquaredOf(first, refinement) + travelSquaredOf(second, refinement) };
}

function travelSquaredOf(reactant: PlacedAtom, refinement: Refinement): number {
    return posedPositionOf(refinement.gathered, reactant).distanceToSquared(posedPositionOf(refinement.gathered, partnerOf(refinement, reactant)));
}

function partnerOf(refinement: Refinement, reactant: PlacedAtom): PlacedAtom {
    const product = refinement.productByReactant.get(reactant);

    if (product === undefined) {
        throw new Error(`Reactant: ${reactant.symbol} of unit ${reactant.unitId} has no product partner`);
    }

    return product;
}

function swapPartners(first: PlacedAtom, second: PlacedAtom, productByReactant: Map<PlacedAtom, PlacedAtom>): void {
    const firstProduct = productByReactant.get(first);
    const secondProduct = productByReactant.get(second);

    if (firstProduct !== undefined && secondProduct !== undefined) {
        productByReactant.set(first, secondProduct);
        productByReactant.set(second, firstProduct);
    }
}

function indexBondsByAtom(bonds: readonly PlacedBond[]): BondsByAtom {
    const bondsByAtom = new Map<PlacedAtom, PlacedBond[]>();

    for (const bond of bonds) {
        addBondEnd(bondsByAtom, bond.from, bond);
        addBondEnd(bondsByAtom, bond.to, bond);
    }

    return bondsByAtom;
}

function addBondEnd(bondsByAtom: Map<PlacedAtom, PlacedBond[]>, atom: PlacedAtom, bond: PlacedBond): void {
    const bondsOfAtom = bondsByAtom.get(atom);

    if (bondsOfAtom) {
        bondsOfAtom.push(bond);
    } else {
        bondsByAtom.set(atom, [bond]);
    }
}