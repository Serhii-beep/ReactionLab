import { computed, Service, signal } from "@angular/core";
import { SubstanceSummary } from "../data/substances/substance";
import { initial, History, canUndo, canRedo, undo, redo, commit } from "./workspace-history";
import { ReactionOutcome, SubstancePortion } from "../data/reactions/reaction-outcome";

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
        this.apply((contents) => withPortions(contents, [{ substance, count: 1 }]));
    }

    addPortions(portions: readonly SubstancePortion[]): void {
        this.apply((contents) => (portions.length === 0 ? contents : withPortions(contents, portions)));
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

    applyOutcome(outcome: ReactionOutcome): void {
        this.apply((contents) => withOutcome(contents, outcome));
    }

    entriesAfter(outcome: ReactionOutcome): readonly WorkspaceItem[] {
        return [...withOutcome(this.contents(), outcome).values()];
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

function withPortions(contents: WorkspaceContents, portions: readonly SubstancePortion[]): WorkspaceContents {
    const next = new Map(contents);

    for (const { substance, count } of portions) {
        const existing = next.get(substance.id);

        next.set(substance.id, { substance, count: (existing?.count ?? 0) + count });
    }

    return next;
}

function withOutcome(contents: WorkspaceContents, outcome: ReactionOutcome): WorkspaceContents {
    const next = new Map(contents);

    for (const { substanceId, count } of outcome.consumed) {
        const existing = next.get(substanceId);

        if (existing === undefined) {
            continue;
        }

        if (existing.count > count) {
            next.set(substanceId, { substance: existing.substance, count: existing.count - count });
        } else {
            next.delete(substanceId);
        }
    }

    return withPortions(next, outcome.produced);
}