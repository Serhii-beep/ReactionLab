import { BondOrder, MoleculeSketch, SketchBond } from "../../../data/molecules/molecule-sketch";

const HYDROGEN = 'H';

export function withAtom(sketch: MoleculeSketch, symbol: string, attachTo: number | null, order: BondOrder, key: number): MoleculeSketch {
    const index = sketch.atoms.length;

    if (attachTo !== null && !isAtomOf(sketch, attachTo)) {
        return sketch;
    }

    return {
        atoms: [...sketch.atoms, { key, symbol, formalCharge: null }],
        bonds: attachTo === null ? sketch.bonds : [...sketch.bonds, { from: attachTo, to: index, order }]
    };
}

export function highestKeyOf(sketch: MoleculeSketch): number {
    return sketch.atoms.reduce((highest, atom) => Math.max(highest, atom.key), -1);
}

export function bondBetween(sketch: MoleculeSketch, first: number, second: number): SketchBond | undefined {
    return sketch.bonds.find((bond) =>
        (bond.from === first && bond.to === second) || (bond.from === second && bond.to === first));
}

export function withBond(sketch: MoleculeSketch, first: number, second: number, order: BondOrder): MoleculeSketch {
    const existing = bondBetween(sketch, first, second);

    if (first === second || !isAtomOf(sketch, first) || !isAtomOf(sketch, second) || existing?.order === order) {
        return sketch;
    }

    return {
        atoms: sketch.atoms,
        bonds: existing
            ? sketch.bonds.map((bond) => (bond === existing ? { ...bond, order } : bond))
            : [...sketch.bonds, { from: first, to: second, order }]
    };
}

export function withoutBond(sketch: MoleculeSketch, first: number, second: number): MoleculeSketch {
    const existing = bondBetween(sketch, first, second);

    return existing ? { atoms: sketch.atoms, bonds: sketch.bonds.filter((bond) => bond !== existing) } : sketch;
}

export function withoutAtom(sketch: MoleculeSketch, removed: number): MoleculeSketch {
    const shifted = (index: number) => (index > removed ? index - 1 : index);

    if (!isAtomOf(sketch, removed)) {
        return sketch;
    }

    return {
        atoms: sketch.atoms.filter((_, index) => index !== removed),
        bonds: sketch.bonds
            .filter((bond) => bond.from !== removed && bond.to !== removed)
            .map((bond) => ({ from: shifted(bond.from), to: shifted(bond.to), order: bond.order }))
    };
}

export function withFormalCharge(sketch: MoleculeSketch, charged: number, formalCharge: number | null): MoleculeSketch {
    if (!isAtomOf(sketch, charged) || sketch.atoms[charged].formalCharge === formalCharge) {
        return sketch;
    }

    return {
        atoms: sketch.atoms.map((atom, index) => (index === charged ? { ...atom, formalCharge } : atom)),
        bonds: sketch.bonds
    };
}

export function withHydrogens(sketch: MoleculeSketch, openValences: readonly number[], firstKey: number): MoleculeSketch {
    let filled = sketch;
    let key = firstKey;

    openValences.forEach((open, atom) => {
        for (let added = 0; added < open; added++) {
            filled = withAtom(filled, HYDROGEN, atom, 1, key++);
        }
    });

    return filled;
}

function isAtomOf(sketch: MoleculeSketch, index: number): boolean {
    return Number.isInteger(index) && index >= 0 && index < sketch.atoms.length;
}
