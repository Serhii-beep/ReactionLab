import { MathUtils, Spherical, Vector3 } from "three";

export interface CameraKey {
    readonly seconds: number;
    readonly target: Vector3;
    readonly framedRadius: number;
    readonly azimuthRadians: number;
    readonly polarRadians: number;
}

type KeyChannel = (key: CameraKey) => number;

const TARGET_X: KeyChannel = (key) => key.target.x;
const TARGET_Y: KeyChannel = (key) => key.target.y;
const TARGET_Z: KeyChannel = (key) => key.target.z;
const FRAMED_RADIUS: KeyChannel = (key) => key.framedRadius;
const AZIMUTH: KeyChannel = (key) => key.azimuthRadians;
const POLAR: KeyChannel = (key) => key.polarRadians;
const MONOTONE_SLOPE_LIMIT = 3;

export class CameraPath {
    readonly nearestFramedRadius: number;

    constructor(private readonly keys: readonly CameraKey[]) {
        this.nearestFramedRadius = Math.min(...keys.map(FRAMED_RADIUS));
    }

    sampleAt(seconds: number, target: Vector3, framedView: Spherical): void {
        const index = this.segmentIndexAt(seconds);
        const start = this.keys[index];
        const end = this.keys[index + 1];
        const progress = MathUtils.clamp((seconds - start.seconds) / (end.seconds - start.seconds), 0, 1);
        const valueOf = (channel: KeyChannel) => this.hermiteAt(index, progress, channel);

        target.set(valueOf(TARGET_X), valueOf(TARGET_Y), valueOf(TARGET_Z));
        framedView.set(valueOf(FRAMED_RADIUS), valueOf(POLAR), valueOf(AZIMUTH));
    }

    private segmentIndexAt(seconds: number): number {
        let index = 0;

        while (index < this.keys.length - 2 && seconds > this.keys[index + 1].seconds) {
            index++;
        }

        return index;
    }

    private hermiteAt(index: number, progress: number, channel: KeyChannel): number {
        const start = this.keys[index];
        const end = this.keys[index + 1];
        const spanSeconds = end.seconds - start.seconds;
        const squared = progress * progress;
        const cubed = squared * progress;
        const startWeight = 2 * cubed - 3 * squared + 1;
        const startSlopeWeight = (cubed - 2 * squared + progress) * spanSeconds;
        const endWeight = 3 * squared - 2 * cubed;
        const endSlopeWeight = (cubed - squared) * spanSeconds;

        return startWeight * channel(start) + startSlopeWeight * this.slopeAt(index, channel)
            + endWeight * channel(end) + endSlopeWeight * this.slopeAt(index + 1, channel);
    }

    private slopeAt(index: number, channel: KeyChannel): number {
        if (index === 0 || index === this.keys.length - 1) {
            return 0;
        }

        const before = this.keys[index - 1];
        const key = this.keys[index];
        const after = this.keys[index + 1];
        const slopeBefore = (channel(key) - channel(before)) / (key.seconds - before.seconds);
        const slopeAfter = (channel(after) - channel(key)) / (after.seconds - key.seconds);

        if (slopeBefore * slopeAfter <= 0) {
            return 0;
        }

        const slopeAcross = (channel(after) - channel(before)) / (after.seconds - before.seconds);
        const limit = MONOTONE_SLOPE_LIMIT * Math.min(Math.abs(slopeBefore), Math.abs(slopeAfter));

        return Math.sign(slopeAcross) * Math.min(Math.abs(slopeAcross), limit);
    }
}