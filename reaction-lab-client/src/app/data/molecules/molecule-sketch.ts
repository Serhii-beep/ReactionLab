export type BondOrder = 1 | 2 | 3;

export interface SketchAtom {
    readonly key: number;
    readonly symbol: string;
    readonly formalCharge: number | null;
}

export interface SketchBond {
    readonly from: number;
    readonly to: number;
    readonly order: BondOrder;
}

export interface MoleculeSketch {
    readonly atoms: readonly SketchAtom[];
    readonly bonds: readonly SketchBond[];
}

export const EMPTY_SKETCH: MoleculeSketch = { atoms: [], bonds: [] };

export const BOND_ORDERS: readonly BondOrder[] = [1, 2, 3];

export function isBondOrder(value: unknown): value is BondOrder {
    return value === 1 || value === 2 || value === 3;
}

export function isMoleculeSketch(value: unknown): value is MoleculeSketch {
    if (!isRecord(value) || !Array.isArray(value['atoms']) || !Array.isArray(value['bonds'])) {
        return false;
    }

    const atomCount = value['atoms'].length;

    return value['atoms'].every(isSketchAtom)
        && value['bonds'].every((bond: unknown) => isSketchBond(bond, atomCount));
}

function isSketchAtom(value: unknown): value is SketchAtom {
    return isRecord(value)
        && typeof value['key'] === 'number'
        && typeof value['symbol'] === 'string'
        && (value['formalCharge'] === null || typeof value['formalCharge'] === 'number');
}

function isSketchBond(value: unknown, atomCount: number): value is SketchBond {
    return isRecord(value)
        && isAtomIndex(value['from'], atomCount)
        && isAtomIndex(value['to'], atomCount)
        && isBondOrder(value['order']);
}

function isAtomIndex(value: unknown, atomCount: number): boolean {
    return typeof value === 'number' && Number.isInteger(value) && value >= 0 && value < atomCount;
}

function isRecord(value: unknown): value is Record<string, unknown> {
    return typeof value === 'object' && value !== null;
}
