import { PlacedAtom, PlacedBond } from "../scene/bench-layout";
import { AtomPair } from "./atom-pairing";

export type BondByAtomPair = ReadonlyMap<PlacedAtom, ReadonlyMap<PlacedAtom, PlacedBond>>;

export type ProductByReactant = ReadonlyMap<PlacedAtom, PlacedAtom>;

export interface SurvivingBond {
    readonly reactant: PlacedBond;
    readonly product: PlacedBond;
}

export interface BondChanges {
    readonly breaking: readonly PlacedBond[];
    readonly surviving: readonly SurvivingBond[];
    readonly forming: readonly PlacedBond[];
}

export function indexBondsByEnds(bonds: readonly PlacedBond[]): BondByAtomPair {
    const bondByEnds = new Map<PlacedAtom, Map<PlacedAtom, PlacedBond>>();

    for (const bond of bonds) {
        indexBondEnd(bondByEnds, bond.from, bond.to, bond);
        indexBondEnd(bondByEnds, bond.to, bond.from, bond);
    }

    return bondByEnds;
}

export function productCounterpartOf(bond: PlacedBond, productByReactant: ProductByReactant, productBondByEnds: BondByAtomPair): PlacedBond | undefined {
    const from = productByReactant.get(bond.from);
    const to = productByReactant.get(bond.to);
    const counterpart = from !== undefined && to !== undefined ? productBondByEnds.get(from)?.get(to) : undefined;

    return counterpart !== undefined && counterpart.kind === bond.kind ? counterpart : undefined;
}

export function bondSurvives(bond: PlacedBond, productByReactant: ProductByReactant, productBondByEnds: BondByAtomPair): boolean {
    return productCounterpartOf(bond, productByReactant, productBondByEnds) !== undefined;
}

export function classifyBonds(
    reactantBonds: readonly PlacedBond[],
    productBonds: readonly PlacedBond[],
    productBondByEnds: BondByAtomPair,
    atomPairs: readonly AtomPair[]
): BondChanges {
    const productByReactant: ProductByReactant = new Map(atomPairs.map((pair) => [pair.reactant, pair.product]));
    const survivingProducts = new Set<PlacedBond>();
    const breaking: PlacedBond[] = [];
    const surviving: SurvivingBond[] = [];

    for (const bond of reactantBonds) {
        const counterpart = productCounterpartOf(bond, productByReactant, productBondByEnds);

        if (counterpart === undefined) {
            breaking.push(bond);
        } else {
            surviving.push({ reactant: bond, product: counterpart });
            survivingProducts.add(counterpart);
        }
    }

    return { breaking, surviving, forming: productBonds.filter((bond) => !survivingProducts.has(bond)) };
}

export function setStrength(bonds: readonly PlacedBond[], strength: number): void {
    for (const bond of bonds) {
        bond.strength = strength;
    }
}

function indexBondEnd(bondByEnds: Map<PlacedAtom, Map<PlacedAtom, PlacedBond>>, from: PlacedAtom, to: PlacedAtom, bond: PlacedBond): void {
    const neighbors = bondByEnds.get(from);

    if (neighbors) {
        neighbors.set(to, bond);
    } else {
        bondByEnds.set(from, new Map([[to, bond]]));
    }
}