import { Vector3 } from "three";

interface ShakeWave {
    readonly hertz: number;
    readonly phaseRadians: number;
    readonly weight: number;
}

const DECAY_SECONDS = 0.32;
const ONSET_SECONDS = 0.02;
const SHAKE_SECONDS = 1.6;
const BASE_STRENGTH = 0.004;
const RELEASE_STRENGTH = 0.018;
const FULL_RELEASE_KILOJOULES_PER_MOLE = 600;
const WAVES_BY_AXIS: readonly (readonly ShakeWave[])[] = [
    [{ hertz: 17, phaseRadians: 0, weight: 1 }, { hertz: 29, phaseRadians: 1.3, weight: 0.5 }],
    [{ hertz: 13, phaseRadians: 0.7, weight: 1 }, { hertz: 23, phaseRadians: 2.1, weight: 0.5 }],
    [{ hertz: 19, phaseRadians: 2.9, weight: 1 }]
];

export class CameraShake {
    private readonly strength: number;
    private readonly endSeconds: number;

    constructor(private readonly startSeconds: number, runEndSeconds: number, enthalpyKilojoulesPerMole: number) {
        const release = Math.min(-enthalpyKilojoulesPerMole / FULL_RELEASE_KILOJOULES_PER_MOLE, 1);

        this.strength = enthalpyKilojoulesPerMole < 0 ? BASE_STRENGTH + RELEASE_STRENGTH * release : 0;
        this.endSeconds = Math.min(startSeconds + SHAKE_SECONDS, runEndSeconds);
    }

    offsetAt(seconds: number, cameraDistance: number, offset: Vector3): Vector3 {
        const sinceSeconds = seconds - this.startSeconds;

        if (this.strength === 0 || sinceSeconds <= 0 || seconds >= this.endSeconds) {
            return offset.set(0, 0, 0);
        }

        const envelope = this.strength * cameraDistance * Math.exp(-sinceSeconds / DECAY_SECONDS) * Math.min(sinceSeconds / ONSET_SECONDS, 1);
        const [alongX, alongY, alongZ] = WAVES_BY_AXIS.map((waves) => waveSumAt(waves, sinceSeconds));

        return offset.set(alongX, alongY, alongZ).multiplyScalar(envelope);
    }
}

function waveSumAt(waves: readonly ShakeWave[], seconds: number): number {
    return waves.reduce((sum, wave) => sum + wave.weight * Math.sin(2 * Math.PI * wave.hertz * seconds + wave.phaseRadians), 0);
}