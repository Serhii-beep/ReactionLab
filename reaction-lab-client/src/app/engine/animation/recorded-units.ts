import { Quaternion, Sphere, Vector3 } from "three";
import { PlacedAtom, PlacedBond } from "../scene/bench-layout";
import { StagedBench } from "./unit-staging";
import { RecordingIndexByAtom } from "./dynamics-input-builder";
import { DynamicsRecording, recordedPositionAt } from "./dynamics/dynamics-recording";
import { TurnSpan, UnitTurns } from "./unit-turns";
import { bondAxis, bondPerpendicular, drawsSideLines } from "../scene/bond-lines";

interface RecordedAtom {
    readonly atom: PlacedAtom;
    readonly recordingIndex: number;
    readonly rest: Vector3;
}

interface RidingAtom {
    readonly atom: PlacedAtom;
    readonly rest: Vector3;
}

interface BondPlane {
    readonly bond: PlacedBond;
    readonly restAxis: Vector3;
    readonly restPerpendicular: Vector3;
}

interface RecordedUnit {
    readonly recorded: readonly RecordedAtom[];
    readonly riding: readonly RidingAtom[];
    readonly recordedRestCentroid: Vector3;
    readonly sphere: Sphere | undefined;
    readonly bondPlanes: readonly BondPlane[];
    readonly turns: UnitTurns | undefined;
}

const TURNED_AXIS = new Vector3();
const SHOWN_AXIS = new Vector3();
const BOND_SWING = new Quaternion();

export class RecordedUnits {
    readonly unitIds: ReadonlySet<string>;

    private readonly recordedUnits: readonly RecordedUnit[];
    private readonly centroid = new Vector3();
    private readonly turn = new Quaternion();

    constructor(bench: StagedBench, recordingIndexByAtom: RecordingIndexByAtom, private readonly recording: DynamicsRecording, turnSpan: TurnSpan) {
        this.unitIds = new Set(bench.atoms.filter((atom) => recordingIndexByAtom.has(atom)).map((atom) => atom.unitId));
        this.recordedUnits = [...this.unitIds].map((unitId) => recordedUnitOf(bench, unitId, recordingIndexByAtom, recording, turnSpan));
    }

    place(seconds: number, restBlend: number): void {
        for (const unit of this.recordedUnits) {
            for (const { atom, recordingIndex, rest } of unit.recorded) {
                recordedPositionAt(this.recording, recordingIndex, seconds, atom.position).lerp(rest, restBlend);
            }

            this.carryRiders(unit);
            this.followAtoms(unit);
            this.turnBondPlanes(unit, seconds, restBlend);
        }
    }

    private carryRiders(unit: RecordedUnit): void {
        if (unit.riding.length === 0) {
            return;
        }

        this.centroid.set(0, 0, 0);

        for (const { atom } of unit.recorded) {
            this.centroid.add(atom.position);
        }

        this.centroid.divideScalar(unit.recorded.length).sub(unit.recordedRestCentroid);

        for (const { atom, rest } of unit.riding) {
            atom.position.copy(rest).add(this.centroid);
        }
    }

    private followAtoms(unit: RecordedUnit): void {
        this.centroid.set(0, 0, 0);

        for (const { atom } of unit.recorded) {
            this.centroid.add(atom.position);
        }

        for (const { atom } of unit.riding) {
            this.centroid.add(atom.position);
        }

        this.centroid.divideScalar(unit.recorded.length + unit.riding.length);
        unit.sphere?.center.copy(this.centroid);
    }

    private turnBondPlanes(unit: RecordedUnit, seconds: number, restBlend: number): void {
        if (unit.turns === undefined) {
            return;
        }

        unit.turns.turnAt(seconds, restBlend, this.turn);

        for (const plane of unit.bondPlanes) {
            turnBondPlane(plane, this.turn);
        }
    }
}

function recordedUnitOf(
    bench: StagedBench,
    unitId: string,
    recordingIndexByAtom: RecordingIndexByAtom,
    recording: DynamicsRecording,
    turnSpan: TurnSpan): RecordedUnit {
    const recorded: RecordedAtom[] = [];
    const riding: RidingAtom[] = [];

    for (const atom of bench.atoms.filter((candidate) => candidate.unitId === unitId)) {
        const recordingIndex = recordingIndexByAtom.get(atom);

        if (recordingIndex === undefined) {
            riding.push({ atom, rest: atom.position.clone() });
        } else {
            recorded.push({ atom, recordingIndex, rest: atom.position.clone() });
        }
    }

    const bondPlanes = bench.bonds.filter((bond) => bond.from.unitId === unitId && drawsSideLines(bond.kind))
        .map((bond) => ({ bond, restAxis: bondAxis(bond), restPerpendicular: bondPerpendicular(bond) }));

    return {
        recorded,
        riding,
        recordedRestCentroid: recorded.reduce((sum, { rest }) => sum.add(rest), new Vector3()).divideScalar(recorded.length),
        sphere: bench.sphereByUnitId.get(unitId),
        bondPlanes,
        turns: bondPlanes.length > 0 ? new UnitTurns(recording, recorded, turnSpan) : undefined
    };
}

function turnBondPlane({ bond, restAxis, restPerpendicular }: BondPlane, turn: Quaternion): void {
    TURNED_AXIS.copy(restAxis).applyQuaternion(turn);
    SHOWN_AXIS.copy(bond.to.position).sub(bond.from.position).normalize();
    BOND_SWING.setFromUnitVectors(TURNED_AXIS, SHOWN_AXIS);
    bond.sidePoint.copy(restPerpendicular).applyQuaternion(turn).applyQuaternion(BOND_SWING).addScaledVector(bond.from.position, 0.5).addScaledVector(bond.to.position, 0.5);
}