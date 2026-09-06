import { Service, signal } from "@angular/core";

@Service()
export class UiStore {
    readonly paletteOpen = signal(false);
    readonly reactionsOpen = signal(false);
    readonly tableOpen = signal(false);
    readonly tableElement = signal<string | null>(null);

    openPalette(): void {
        this.paletteOpen.set(true);
    }

    openReactions(): void {
        this.tableOpen.set(false);
        this.reactionsOpen.set(true);
    }

    openTable(symbol: string | null = null): void {
        if (symbol !== null) {
            this.tableElement.set(symbol);
        }

        this.reactionsOpen.set(false);
        this.tableOpen.set(true);
    }

    toggleTable(): void {
        if (this.tableOpen()) {
            this.tableOpen.set(false);
        } else {
            this.openTable();
        }
    }

    dismiss(): void {
        this.reactionsOpen.set(false);
        this.tableOpen.set(false);
    }
}