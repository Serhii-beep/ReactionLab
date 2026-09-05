import { Service, signal } from "@angular/core";

@Service()
export class UiStore {
    readonly paletteOpen = signal(false);

    openPalette(): void {
        this.paletteOpen.set(true);
    }
}