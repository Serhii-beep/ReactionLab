import { Injectable, signal } from "@angular/core";

@Injectable()
export class SceneViewport {
    private readonly fits = signal(0);

    readonly fitRequests = this.fits.asReadonly();

    requestFit(): void {
        this.fits.update((count) => count + 1);
    }
}