import { PlacedAtom } from "../scene/bench-layout";
import { PosedPoints, posedPositionOf } from "./unit-gathering";

export interface AtomPair {
    readonly reactant: PlacedAtom;
    readonly product: PlacedAtom;
}

interface CandidatePair extends AtomPair {
    readonly distanceSquared: number;
}

type AtomsBySymbol = ReadonlyMap<string, readonly PlacedAtom[]>;

export function pairAtoms(reactantAtoms: readonly PlacedAtom[], productAtoms: readonly PlacedAtom[], gathered: PosedPoints): AtomPair[] {
    const productsBySymbol = groupBySymbol(productAtoms);
    const atomPairs: AtomPair[] = [];

    for (const [symbol, reactantsOfElement] of groupBySymbol(reactantAtoms)) {
        atomPairs.push(...pairAtomsOfElement(reactantsOfElement, productsBySymbol.get(symbol) ?? [], gathered));
    }

    return atomPairs;
}

function pairAtomsOfElement(reactantAtoms: readonly PlacedAtom[], productAtoms: readonly PlacedAtom[], gathered: PosedPoints): AtomPair[] {
    const pairedReactants = new Set<PlacedAtom>();
    const pairedProducts = new Set<PlacedAtom>();
    const atomPairs: AtomPair[] = [];

    for (const { reactant, product } of candidatePairsByDistance(reactantAtoms, productAtoms, gathered)) {
        if (!pairedReactants.has(reactant) && !pairedProducts.has(product)) {
            atomPairs.push({ reactant, product });
            pairedReactants.add(reactant);
            pairedProducts.add(product);
        }
    }

    return atomPairs;
}

function candidatePairsByDistance(reactantAtoms: readonly PlacedAtom[], productAtoms: readonly PlacedAtom[], gathered: PosedPoints): CandidatePair[] {
    const candidates: CandidatePair[] = [];

    for (const reactant of reactantAtoms) {
        const reactantPosition = posedPositionOf(gathered, reactant);

        for (const product of productAtoms) {
            candidates.push({ reactant, product, distanceSquared: reactantPosition.distanceToSquared(posedPositionOf(gathered, product)) });
        }
    }

    return candidates.sort((first, second) => first.distanceSquared - second.distanceSquared);
}

function groupBySymbol(atoms: readonly PlacedAtom[]): AtomsBySymbol {
    const atomsBySymbol = new Map<string, PlacedAtom[]>();

    for (const atom of atoms) {
        const group = atomsBySymbol.get(atom.symbol);

        if (group) {
            group.push(atom);
        } else {
            atomsBySymbol.set(atom.symbol, [atom]);
        }
    }

    return atomsBySymbol;
}