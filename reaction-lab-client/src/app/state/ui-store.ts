import { Service, signal } from "@angular/core";

@Service()
export class UiStore {
    readonly paletteOpen = signal(false);
    readonly reactionsOpen = signal(false);

    openPalette(): void {
        this.paletteOpen.set(true);
    }

    openReactions(): void {
        this.reactionsOpen.set(true);
    }

    dismiss(): void {
        this.reactionsOpen.set(false);
    }
}