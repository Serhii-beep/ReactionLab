import { computed, Injectable, signal } from "@angular/core";
import { canRedo, canUndo, commit, History, initial, redo, undo } from "../../../state/workspace-history";
import { BondOrder, EMPTY_SKETCH, MoleculeSketch } from "../../../data/molecules/molecule-sketch";
import { highestKeyOf, withAtom, withBond, withFormalCharge, withHydrogens, withoutAtom, withoutBond } from "./sketch-edits";
import { isConnected } from "./constitution-match";
import { indexOfKey, keyAt, KeyedSelection, keyedSelectionOf, resolvedSelection, selectedAtomOf, SketchSelection } from "./sketch-selection";

interface SketchRevision {
    readonly sketch: MoleculeSketch;
    readonly lineage: number;
}

@Injectable()
export class SketchEditor {
    private readonly history = signal<History<SketchRevision>>(initial({ sketch: EMPTY_SKETCH, lineage: 0 }));
    private readonly requestedSelection = signal<KeyedSelection | null>(null);
    private readonly anchorKey = signal<number | null>(null);

    private nextKey = 0;
    private lineages = 0;

    readonly sketch = computed(() => this.history().present.sketch);
    readonly lineage = computed(() => this.history().present.lineage);
    readonly selection = computed(() => resolvedSelection(this.requestedSelection(), this.sketch()));
    readonly selectedAtom = computed(() => selectedAtomOf(this.selection()));
    readonly connectingFrom = computed(() => {
        const key = this.anchorKey();
        const index = key === null ? -1 : indexOfKey(this.sketch(), key);

        return index < 0 ? null : index;
    });
    readonly connecting = computed(() => this.connectingFrom() !== null);
    readonly bondOrder = signal<BondOrder>(1);
    readonly isEmpty = computed(() => this.sketch().atoms.length === 0);
    readonly connected = computed(() => isConnected(this.sketch().atoms.length, this.sketch().bonds));
    readonly canUndo = computed(() => canUndo(this.history()));
    readonly canRedo = computed(() => canRedo(this.history()));

    add(symbol: string): void {
        this.apply(withAtom(this.sketch(), symbol, this.selectedAtom(), this.bondOrder(), this.nextKey), this.newAtomSelection());
    }

    attachTo(owner: number, symbol: string): void {
        this.apply(withAtom(this.sketch(), symbol, owner, this.bondOrder(), this.nextKey), this.newAtomSelection());
    }

    pickAtom(atom: number, additive: boolean): void {
        const anchor = this.connectingFrom() ?? (additive ? this.selectedAtom() : null);

        if (anchor !== null && anchor !== atom) {
            this.bond(anchor, atom);
        } else if (anchor === atom && this.connecting()) {
            this.stopConnecting();
        } else {
            this.select(this.selectedAtom() === atom ? null : { kind: 'atom', atom });
        }
    }

    pickBond(from: number, to: number): void {
        this.stopConnecting();
        this.select({ kind: 'bond', from, to });
    }

    clearSelection(): void {
        this.stopConnecting();
        this.select(null);
    }

    stepSelection(step: number): void {
        const count = this.sketch().atoms.length;
        const current = this.selectedAtom();

        if (count > 0) {
            this.select({ kind: 'atom', atom: current === null ? (step > 0 ? 0 : count - 1) : (current + step + count) % count });
        }
    }

    startConnecting(): void {
        this.anchorKey.set(keyAt(this.sketch(), this.selectedAtom() ?? -1));
    }

    stopConnecting(): void {
        this.anchorKey.set(null);
    }

    link(): void {
        const anchor = this.connectingFrom();
        const selected = this.selectedAtom();

        if (anchor === null) {
            this.startConnecting();
        } else if (selected !== null && selected !== anchor) {
            this.bond(anchor, selected);
        } else {
            this.stopConnecting();
        }
    }

    setBondOrder(order: BondOrder): void {
        const selection = this.selection();

        this.bondOrder.set(order);

        if (selection?.kind === 'bond') {
            this.apply(withBond(this.sketch(), selection.from, selection.to, order), selection);
        }
    }

    setFormalCharge(formalCharge: number | null): void {
        const atom = this.selectedAtom();

        if (atom !== null) {
            this.apply(withFormalCharge(this.sketch(), atom, formalCharge), this.selection());
        }
    }

    removeSelected(): void {
        const selection = this.selection();

        if (selection?.kind === 'atom') {
            this.apply(withoutAtom(this.sketch(), selection.atom), null);
        } else if (selection?.kind === 'bond') {
            this.apply(withoutBond(this.sketch(), selection.from, selection.to), null);
        }
    }

    fillHydrogens(openValences: readonly number[]): void {
        this.apply(withHydrogens(this.sketch(), openValences, this.nextKey), this.selection());
    }

    load(sketch: MoleculeSketch): void {
        this.lineages += 1;
        this.nextKey = Math.max(this.nextKey, highestKeyOf(sketch) + 1);
        this.history.update((history) => commit(history, { sketch, lineage: this.lineages }));
        this.requestedSelection.set(null);
        this.stopConnecting();
    }

    undo(): void {
        this.history.update(undo);
    }

    redo(): void {
        this.history.update(redo);
    }

    private bond(from: number, to: number): void {
        this.stopConnecting();
        this.apply(withBond(this.sketch(), from, to, this.bondOrder()), { kind: 'bond', from, to });
    }

    private select(selection: SketchSelection | null): void {
        this.requestedSelection.set(keyedSelectionOf(selection, this.sketch()));
    }

    private newAtomSelection(): SketchSelection {
        return { kind: 'atom', atom: this.sketch().atoms.length };
    }

    private apply(next: MoleculeSketch, selection: SketchSelection | null): void {
        if (next !== this.sketch()) {
            this.nextKey = Math.max(this.nextKey, highestKeyOf(next) + 1);
            this.history.update((history) => commit(history, { sketch: next, lineage: history.present.lineage }));
        }

        this.requestedSelection.set(keyedSelectionOf(selection, next));
    }
}
