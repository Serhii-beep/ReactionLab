import { MoleculeSketch } from "../../../data/molecules/molecule-sketch";
import { bondBetween } from "./sketch-edits";

export type SketchSelection =
    | { readonly kind: 'atom'; readonly atom: number }
    | { readonly kind: 'bond'; readonly from: number; readonly to: number };

export type KeyedSelection =
    | { readonly kind: 'atom'; readonly key: number }
    | { readonly kind: 'bond'; readonly fromKey: number; readonly toKey: number };

export function keyedSelectionOf(selection: SketchSelection | null, sketch: MoleculeSketch): KeyedSelection | null {
    if (selection?.kind === 'atom') {
        const key = keyAt(sketch, selection.atom);

        return key === null ? null : { kind: 'atom', key };
    }

    if (selection?.kind === 'bond') {
        const fromKey = keyAt(sketch, selection.from);
        const toKey = keyAt(sketch, selection.to);

        return fromKey === null || toKey === null ? null : { kind: 'bond', fromKey, toKey };
    }

    return null;
}

export function keyAt(sketch: MoleculeSketch, index: number): number | null {
    return index >= 0 && index < sketch.atoms.length ? sketch.atoms[index].key : null;
}

export function resolvedSelection(selection: KeyedSelection | null, sketch: MoleculeSketch): SketchSelection | null {
    if (selection?.kind === 'atom') {
        const atom = indexOfKey(sketch, selection.key);

        return atom < 0 ? null : { kind: 'atom', atom };
    }

    if (selection?.kind === 'bond') {
        const from = indexOfKey(sketch, selection.fromKey);
        const to = indexOfKey(sketch, selection.toKey);

        return from >= 0 && to >= 0 && bondBetween(sketch, from, to) ? { kind: 'bond', from, to } : null;
    }

    return null;
}

export function indexOfKey(sketch: MoleculeSketch, key: number): number {
    return sketch.atoms.findIndex((atom) => atom.key === key);
}

export function selectedAtomOf(selection: SketchSelection | null): number | null {
    return selection?.kind === 'atom' ? selection.atom : null;
}
