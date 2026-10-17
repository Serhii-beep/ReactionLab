import { Service, signal } from "@angular/core";

@Service()
export class UiStore {
    readonly paletteOpen = signal(false);
    readonly reactionsOpen = signal(false);
    readonly tableOpen = signal(false);
    readonly tableElement = signal<string | null>(null);
    readonly aboutOpen = signal(false);
    readonly aboutSubstanceId = signal<string | null>(null);
    readonly builderOpen = signal(false);
    readonly conditionsOpen = signal(false);

    openPalette(): void {
        this.paletteOpen.set(true);
    }

    openBuilder(): void {
        this.dismiss();
        this.paletteOpen.set(false);
        this.builderOpen.set(true);
    }

    closeBuilder(): void {
        this.builderOpen.set(false);
    }

    openConditions(): void {
        this.reactionsOpen.set(false);
        this.tableOpen.set(false);
        this.aboutOpen.set(false);
        this.conditionsOpen.set(true);
    }

    closeConditions(): void {
        this.conditionsOpen.set(false);
    }

    openReactions(): void {
        this.tableOpen.set(false);
        this.aboutOpen.set(false);
        this.conditionsOpen.set(false);
        this.reactionsOpen.set(true);
    }

    openTable(symbol: string | null = null): void {
        if (symbol !== null) {
            this.tableElement.set(symbol);
        }

        this.reactionsOpen.set(false);
        this.aboutOpen.set(false);
        this.conditionsOpen.set(false);
        this.tableOpen.set(true);
    }

    openAbout(substanceId: string): void {
        this.aboutSubstanceId.set(substanceId);
        this.reactionsOpen.set(false);
        this.tableOpen.set(false);
        this.conditionsOpen.set(false);
        this.aboutOpen.set(true);
    }

    closeAbout(): void {
        this.aboutOpen.set(false);
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
        this.aboutOpen.set(false);
        this.conditionsOpen.set(false);
        this.builderOpen.set(false);
    }
}