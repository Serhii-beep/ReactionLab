import { computed, Service, signal } from "@angular/core";
import { SubstanceSummary } from "../data/substances/substance";
import { initial, History, canUndo, canRedo, undo, redo, commit } from "./workspace-history";

export interface WorkspaceItem {
    readonly substance: SubstanceSummary;
    readonly count: number;
}

export type WorkspaceContents = ReadonlyMap<string, WorkspaceItem>;

const EMPTY: WorkspaceContents = new Map();

@Service()
export class WorkspaceStore {
    private readonly history = signal<History<WorkspaceContents>>(initial(EMPTY));

    private readonly contents = computed(() => this.history().present);

    readonly entries = computed(() => [...this.contents().values()]);
    readonly substanceIds = computed(() => [...this.contents().keys()]);
    readonly counts = computed(() => new Map([...this.contents()].map(([id, item]) => [id, item.count])));
    readonly isEmpty = computed(() => this.contents().size === 0);

    readonly canUndo = computed(() => canUndo(this.history()));
    readonly canRedo = computed(() => canRedo(this.history()));

    add(substance: SubstanceSummary): void {
        this.apply((contents) => {
            const next = new Map(contents);
            const existing = next.get(substance.id);

            next.set(substance.id, { substance, count: (existing?.count ?? 0) + 1 });

            return next;
        });
    }

    removeOne(substanceId: string): void {
        this.apply((contents) => {
            const existing = contents.get(substanceId);

            if (existing === undefined) {
                return contents;
            }

            const next = new Map(contents);

            if (existing.count > 1) {
                next.set(substanceId, { substance: existing.substance, count: existing.count - 1 });
            } else {
                next.delete(substanceId);
            }

            return next;
        });
    }

    clear(): void {
        this.apply((contents) => (contents.size === 0 ? contents : EMPTY));
    }

    undo(): void {
        this.history.update(undo);
    }

    redo(): void {
        this.history.update(redo);
    }

    private apply(change: (contents: WorkspaceContents) => WorkspaceContents): void {
        this.history.update((history) => commit(history, change(history.present)));
    }
}