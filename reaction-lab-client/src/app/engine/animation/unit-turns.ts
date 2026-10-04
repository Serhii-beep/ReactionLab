import { Quaternion, Vector3 } from "three";
import { DynamicsRecording } from "./dynamics/dynamics-recording";
import { continuedBestFitRotation } from "../core/best-fit-rotation";

export interface TurnSpan {
    readonly restSeconds: number;
    readonly towardSeconds: number;
}

export interface TurningAtom {
    readonly recordingIndex: number;
    readonly rest: Vector3;
}

const MAX_LINEAR_DEVIATION = 1e-3;
const REST_TURN = new Quaternion();
const SHAPE_CENTER = new Vector3();
const TURNED_AXIS = new Vector3();
const SHOWN_AXIS = new Vector3();
const AXIS_SWING = new Quaternion();

export class UnitTurns {
    private readonly turnByFrame: Float32Array;
    private readonly lowerTurn = new Quaternion();
    private readonly upperTurn = new Quaternion();

    constructor(private readonly recording: DynamicsRecording, atoms: readonly TurningAtom[], span: TurnSpan) {
        this.turnByFrame = bakedTurns(recording, atoms, span);
    }

    turnAt(seconds: number, restBlend: number, target: Quaternion): void {
        const lastFrame = this.recording.frames.length - 1;
        const framePosition = Math.min(Math.max(seconds, 0) * this.recording.framesPerSecond, lastFrame);
        const lowerFrame = Math.floor(framePosition);

        this.lowerTurn.fromArray(this.turnByFrame, lowerFrame * 4);
        this.upperTurn.fromArray(this.turnByFrame, Math.min(lowerFrame + 1, lastFrame) * 4);

        target.slerpQuaternions(this.lowerTurn, this.upperTurn, framePosition - lowerFrame).slerp(REST_TURN, restBlend);
    }
}

function bakedTurns(recording: DynamicsRecording, atoms: readonly TurningAtom[], span: TurnSpan): Float32Array {
    const { frames, framesPerSecond } = recording;
    const turnByFrame = new Float32Array(frames.length * 4);
    const restCenter = atoms.reduce((sum, { rest }) => sum.add(rest), new Vector3()).divideScalar(atoms.length);
    const restShape = atoms.map(({ rest }) => rest.clone().sub(restCenter));
    const currentShape = atoms.map(() => new Vector3());
    const restAxis = linearAxisOf(restShape);
    const step = span.towardSeconds < span.restSeconds ? -1 : 1;
    const restFrame = nearestFrame(span.restSeconds * framesPerSecond, frames.length);
    const bakedFrameCount = Math.abs(nearestFrame(span.towardSeconds * framesPerSecond + step, frames.length) - restFrame) + 1;
    const turn = new Quaternion();

    for (let stepsFromRest = 0; stepsFromRest < bakedFrameCount; stepsFromRest++) {
        const frame = restFrame + stepsFromRest * step;

        fillShape(frames[frame], atoms, currentShape);

        if (restAxis === null) {
            continuedBestFitRotation(restShape, currentShape, turn, turn);
        } else {
            transportAlongAxis(restAxis, restShape, currentShape, turn);
        }

        turn.toArray(turnByFrame, frame * 4);
    }

    return turnByFrame;
}

function linearAxisOf(restShape: readonly Vector3[]): Vector3 | null {
    const longestOffset = restShape.reduce((longest, offset) => (offset.lengthSq() > longest.lengthSq() ? offset : longest));
    const axis = longestOffset.clone().normalize();

    return restShape.every((offset) => offset.clone().cross(axis).length() <= MAX_LINEAR_DEVIATION) ? axis : null;
}

function transportAlongAxis(restAxis: Vector3, restShape: readonly Vector3[], currentShape: readonly Vector3[], turn: Quaternion): void {
    SHOWN_AXIS.set(0, 0, 0);
    restShape.forEach((offset, index) => SHOWN_AXIS.addScaledVector(currentShape[index], offset.dot(restAxis)));
    TURNED_AXIS.copy(restAxis).applyQuaternion(turn);
    turn.premultiply(AXIS_SWING.setFromUnitVectors(TURNED_AXIS, SHOWN_AXIS.normalize()));
}

function nearestFrame(framePosition: number, frameCount: number): number {
    return Math.min(Math.max(Math.round(framePosition), 0), frameCount - 1);
}

function fillShape(frame: Float32Array, atoms: readonly TurningAtom[], shape: readonly Vector3[]): void {
    SHAPE_CENTER.set(0, 0, 0);
    atoms.forEach(({ recordingIndex }, index) => SHAPE_CENTER.add(shape[index].fromArray(frame, recordingIndex * 3)));
    SHAPE_CENTER.divideScalar(atoms.length);

    for (const offset of shape) {
        offset.sub(SHAPE_CENTER);
    }
}