import { Injectable, signal } from "@angular/core";

@Injectable()
export class SceneViewport {
    private readonly fits = signal(0);
    private readonly benchCovered = signal(0);

    readonly fitRequests = this.fits.asReadonly();
    readonly benchCoveredPixels = this.benchCovered.asReadonly();

    requestFit(): void {
        this.fits.update((count) => count + 1);
    }

    setBenchCoveredPixels(pixels: number): void {
        this.benchCovered.set(pixels);
    }
}