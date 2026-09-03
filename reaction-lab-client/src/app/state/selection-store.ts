import { computed, inject, Service, signal } from "@angular/core";
import { WorkspaceStore } from "./workspace-store";

@Service()
export class SelectionStore {
    private readonly workspace = inject(WorkspaceStore);
    private readonly requested = signal<string | null>(null);

    readonly selectedId = computed(() => {
        const id = this.requested();

        return id !== null && this.workspace.counts().has(id) ? id : null;
    });

    readonly hasSelection = computed(() => this.selectedId() !== null);

    isSelected(substanceId: string): boolean {
        return this.selectedId() === substanceId;
    }

    toggle(substanceId: string): void {
        this.requested.update((current) => (current === substanceId ? null : substanceId));
    }

    clear(): void {
        this.requested.set(null);
    }
}