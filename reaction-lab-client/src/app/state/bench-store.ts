import { computed, Service, signal } from "@angular/core";
import { SubstanceSummary } from "../data/substances/substance";

export interface BenchEntry {
    readonly substance: SubstanceSummary;
    readonly count: number;
}

@Service()
export class BenchStore {
    private readonly items = signal<ReadonlyMap<string, BenchEntry>>(new Map());

    readonly entries = computed(() => [...this.items().values()]);
    readonly substanceIds = computed(() => [...this.items().keys()]);
    readonly counts = computed(() => new Map([...this.items()].map(([id, entry]) => [id, entry.count])));
    readonly isEmpty = computed(() => this.items().size === 0);

    add(substance: SubstanceSummary): void {
        this.items.update((items) => {
            const next = new Map(items);
            const existing = next.get(substance.id);

            next.set(substance.id, { substance, count: (existing?.count ?? 0) + 1 });

            return next;
        });
    }

    removeOne(substanceId: string): void {
        this.items.update((items) => {
            const existing = items.get(substanceId);

            if (existing === undefined) {
                return items;
            }

            const next = new Map(items);

            if (existing.count > 1) {
                next.set(substanceId, { substance: existing.substance, count: existing.count - 1 });
            } else {
                next.delete(substanceId);
            }

            return next;
        });
    }

    clear(): void {
        this.items.set(new Map());
    }
}