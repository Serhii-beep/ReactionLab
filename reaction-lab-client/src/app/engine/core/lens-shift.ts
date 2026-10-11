import { MathUtils, PerspectiveCamera } from "three";

export type LensShiftListener = (shiftPixels: number, viewportWidth: number, viewportHeight: number) => void;

const SHIFT_RATE_PER_SECOND = 10;
const SETTLED_PIXELS = 0.5;
const SMALLEST_HEIGHT_SHARE = 0.3;

export class LensShift {
    private readonly listeners = new Set<LensShiftListener>();

    private viewportWidth = 0;
    private viewportHeight = 0;
    private targetCoveredPixels = 0;
    private shownCoveredPixels = 0;

    constructor(private readonly camera: PerspectiveCamera) {}

    get targetHeightShare(): number {
        return this.heightShareOf(this.targetCoveredPixels);
    }

    get shownHeightShare(): number {
        return this.heightShareOf(this.shownCoveredPixels);
    }

    onShift(listener: LensShiftListener): () => void {
        this.listeners.add(listener);

        return () => this.listeners.delete(listener);
    }

    setViewportSize(width: number, height: number): void {
        this.viewportWidth = width;
        this.viewportHeight = height;
        this.apply();
    }

    cover(coveredPixels: number, animated: boolean): void {
        this.targetCoveredPixels = coveredPixels;

        if (!animated) {
            this.shownCoveredPixels = coveredPixels;
            this.apply();
        }
    }

    update(deltaSeconds: number): boolean {
        if (this.shownCoveredPixels === this.targetCoveredPixels) {
            return false;
        }

        const settled = Math.abs(this.targetCoveredPixels - this.shownCoveredPixels) < SETTLED_PIXELS;

        this.shownCoveredPixels = settled
            ? this.targetCoveredPixels
            : MathUtils.damp(this.shownCoveredPixels, this.targetCoveredPixels, SHIFT_RATE_PER_SECOND, deltaSeconds);
        this.apply();

        return true;
    }

    private heightShareOf(coveredPixels: number): number {
        return this.viewportHeight > 0 ? Math.max(1 - coveredPixels / this.viewportHeight, SMALLEST_HEIGHT_SHARE) : 1;
    }

    private apply(): void {
        const shiftPixels = ((1 - this.shownHeightShare) * this.viewportHeight) / 2;

        if (shiftPixels > 0 && this.viewportWidth > 0) {
            this.camera.setViewOffset(this.viewportWidth, this.viewportHeight, 0, shiftPixels, this.viewportWidth, this.viewportHeight);
        } else {
            this.camera.clearViewOffset();
        }

        for (const listener of this.listeners) {
            listener(shiftPixels, this.viewportWidth, this.viewportHeight);
        }
    }
}