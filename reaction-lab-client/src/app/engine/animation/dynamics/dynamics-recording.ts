import { Vector3 } from "three";
import { DynamicsSchedule } from "./dynamics-schedule";

export interface DynamicsRecording {
    readonly framesPerSecond: number;
    readonly frames: readonly Float32Array[];
    readonly schedule: DynamicsSchedule;
    readonly enthalpyKilojoulesPerMole: number;
}

const REACH_SCRATCH = new Vector3();

export function recordedReachAt(recording: DynamicsRecording, atomRadii: readonly number[], seconds: number, center: Vector3): number {
    return atomRadii.reduce((reach, radius, atomIndex) => Math.max(reach, recordedPositionAt(recording, atomIndex, seconds, REACH_SCRATCH).distanceTo(center) + radius), 0);
}

export function recordedPositionAt(recording: DynamicsRecording, atomIndex: number, seconds: number, target: Vector3): Vector3 {
    const { frames, framesPerSecond } = recording;
    const framePosition = Math.min(Math.max(seconds, 0) * framesPerSecond, frames.length - 1);
    const lower = Math.floor(framePosition);
    const upper = Math.min(lower + 1, frames.length - 1);
    const amount = framePosition - lower;
    const slot = atomIndex * 3;

    return target.set(
        frames[lower][slot] + (frames[upper][slot] - frames[lower][slot]) * amount,
        frames[lower][slot + 1] + (frames[upper][slot + 1] - frames[lower][slot + 1]) * amount,
        frames[lower][slot + 2] + (frames[upper][slot + 2] - frames[lower][slot + 2]) * amount
    );
}