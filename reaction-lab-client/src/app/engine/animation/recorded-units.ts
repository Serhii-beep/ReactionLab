import { Sphere, Vector3 } from "three";
import { PlacedAtom, PlacedBond } from "../scene/bench-layout";
import { StagedBench } from "./unit-staging";
import { RecordingIndexByAtom } from "./dynamics-input-builder";
import { DynamicsRecording, recordedPositionAt } from "./dynamics/dynamics-recording";

interface RecordedAtom {
    readonly atom: PlacedAtom;
    readonly recordingIndex: number;
    readonly rest: Vector3;
}

interface RidingAtom {
    readonly atom: PlacedAtom;
    readonly rest: Vector3;
}

interface RecordedUnit {
    readonly recorded: readonly RecordedAtom[];
    readonly riding: readonly RidingAtom[];
    readonly recordedRestCentroid: Vector3;
    readonly sphere: Sphere | undefined;
    readonly bonds: readonly PlacedBond[];
}

export class RecordedUnits {
    readonly unitIds: ReadonlySet<string>;

    private readonly recordedUnits: readonly RecordedUnit[];
    private readonly centroid = new Vector3();

    constructor(bench: StagedBench, recordingIndexByAtom: RecordingIndexByAtom) {
        this.unitIds = new Set(bench.atoms.filter((atom) => recordingIndexByAtom.has(atom)).map((atom) => atom.unitId));
        this.recordedUnits = [...this.unitIds].map((unitId) => recordedUnitOf(bench, unitId, recordingIndexByAtom));
    }

    place(recording: DynamicsRecording, seconds: number, restBlend: number): void {
        for (const unit of this.recordedUnits) {
            for (const { atom, recordingIndex, rest } of unit.recorded) {
                recordedPositionAt(recording, recordingIndex, seconds, atom.position).lerp(rest, restBlend);
            }

            this.carryRiders(unit);
            this.followAtoms(unit);
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

        for (const bond of unit.bonds) {
            bond.centroid.copy(this.centroid);
        }
    }
}

function recordedUnitOf(bench: StagedBench, unitId: string, recordingIndexByAtom: RecordingIndexByAtom): RecordedUnit {
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

    return {
        recorded,
        riding,
        recordedRestCentroid: recorded.reduce((sum, { rest }) => sum.add(rest), new Vector3()).divideScalar(recorded.length),
        sphere: bench.sphereByUnitId.get(unitId),
        bonds: bench.bonds.filter((bond) => bond.from.unitId === unitId)
    };
}