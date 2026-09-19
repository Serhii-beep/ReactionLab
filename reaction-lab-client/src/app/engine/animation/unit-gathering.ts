import { Sphere, Vector3 } from "three";
import { BenchLayout, packUnits, PlacedAtom, PlacedBond } from "../scene/bench-layout";

export interface StagedBench {
    readonly atoms: readonly PlacedAtom[];
    readonly bonds: readonly PlacedBond[];
    readonly sphereByUnitId: ReadonlyMap<string, Sphere>;
}

export interface MovingPoint {
    readonly live: Vector3;
    readonly resting: Vector3;
    readonly atom: PlacedAtom | null;
    readonly bond: PlacedBond | null;
}

export interface UnitShifts {
    readonly fromShift: Vector3;
    readonly toShift: Vector3;
}

export interface UnitTravel extends UnitShifts {
    readonly unitId: string;
    readonly points: readonly MovingPoint[];
    readonly appliedShift: Vector3;
}

export interface StagedSet extends StagedBench {
    readonly travels: readonly UnitTravel[];
}

export interface PosedPoints {
    readonly positionByAtom: ReadonlyMap<PlacedAtom, Vector3>;
    readonly centroidByBond: ReadonlyMap<PlacedBond, Vector3>;
}

export type CentersByUnitId = ReadonlyMap<string, Vector3>;

export type ShiftsFor = (unitId: string, sphere: Sphere) => UnitShifts | null;

export function stageTravellingUnits(layout: BenchLayout, shiftsFor: ShiftsFor): StagedSet {
    const travels: UnitTravel[] = [];

    for (const [unitId, sphere] of layout.sphereByUnitId) {
        const shifts = shiftsFor(unitId, sphere);

        if (shifts !== null) {
            travels.push({ ...shifts, unitId, points: movingPointsOf(layout, unitId, sphere), appliedShift: new Vector3() });
        }
    }

    return { atoms: layout.atoms, bonds: layout.bonds, sphereByUnitId: layout.sphereByUnitId, travels };
}

export function poseUnits(stagedSet: StagedSet, progress: number): void {
    for (const travel of stagedSet.travels) {
        travel.appliedShift.lerpVectors(travel.fromShift, travel.toShift, progress);

        for (const point of travel.points) {
            point.live.copy(point.resting).add(travel.appliedShift);
        }
    }
}

export function pointsAtProgress(stagedSet: StagedSet, progress: number): PosedPoints {
    const positionByAtom = new Map<PlacedAtom, Vector3>();
    const centroidByBond = new Map<PlacedBond, Vector3>();

    for (const travel of stagedSet.travels) {
        const shift = new Vector3().lerpVectors(travel.fromShift, travel.toShift, progress);

        for (const point of travel.points) {
            if (point.atom !== null) {
                positionByAtom.set(point.atom, point.resting.clone().add(shift));
            } else if (point.bond !== null) {
                centroidByBond.set(point.bond, point.resting.clone().add(shift));
            }
        }
    }

    return { positionByAtom, centroidByBond };
}

export function mergePosedPoints(first: PosedPoints, second: PosedPoints): PosedPoints {
    return {
        positionByAtom: new Map([...first.positionByAtom, ...second.positionByAtom]),
        centroidByBond: new Map([...first.centroidByBond, ...second.centroidByBond])
    };
}

export function posedPositionOf(posed: PosedPoints, atom: PlacedAtom): Vector3 {
    const position = posed.positionByAtom.get(atom);

    if (position === undefined) {
        throw new Error(`No posed position for ${atom.symbol} of unit ${atom.unitId}`);
    }

    return position;
}

export function posedCentroidOf(posed: PosedPoints, bond: PlacedBond): Vector3 {
    const centroid = posed.centroidByBond.get(bond);

    if (centroid === undefined) {
        throw new Error(`No posed centroid for a ${bond.kind} bond of unit ${bond.from.unitId}`);
    }

    return centroid;
}

export function atomsOfUnits(bench: StagedBench, unitIds: ReadonlySet<string>): PlacedAtom[] {
    return bench.atoms.filter((atom) => unitIds.has(atom.unitId));
}

export function bondsOfUnits(bench: StagedBench, unitIds: ReadonlySet<string>): PlacedBond[] {
    return bench.bonds.filter((bond) => unitIds.has(bond.from.unitId) || unitIds.has(bond.to.unitId));
}

export function meetingPointOf(layout: BenchLayout, unitIds: readonly string[]): Vector3 {
    if (unitIds.length === 0) {
        return layout.bounds.getCenter(new Vector3()).setY(0);
    }

    const meeting = new Vector3();

    for (const unitId of unitIds) {
        const sphere = layout.sphereByUnitId.get(unitId);

        if (sphere) {
            meeting.add(sphere.center);
        }
    }

    meeting.divideScalar(unitIds.length);
    meeting.y = 0;

    return meeting;
}

export function gatheredCentersOf(layout: BenchLayout, unitIds: readonly string[], meeting: Vector3): CentersByUnitId {
    const spheres = unitIds.flatMap((unitId) => {
        const sphere = layout.sphereByUnitId.get(unitId);

        return sphere ? [{ unitId, sphere }] : [];
    });

    const packed = packUnits(spheres.map(({ sphere }) => sphere.radius), 0);
    const centerByUnitId = new Map<string, Vector3>();

    spheres.forEach(({ unitId, sphere }, index) => {
        const slot = packed.centers[index];

        centerByUnitId.set(unitId, new Vector3(meeting.x + slot.x, sphere.center.y, meeting.z + slot.z));
    });

    return centerByUnitId;
}

function movingPointsOf(layout: BenchLayout, unitId: string, sphere: Sphere): MovingPoint[] {
    const points: MovingPoint[] = [{ live: sphere.center, resting: sphere.center.clone(), atom: null, bond: null }];

    for (const atom of layout.atoms) {
        if (atom.unitId === unitId) {
            points.push({ live: atom.position, resting: atom.position.clone(), atom, bond: null });
        }
    }

    for (const bond of layout.bonds) {
        if (bond.from.unitId === unitId) {
            points.push({ live: bond.centroid, resting: bond.centroid.clone(), atom: null, bond });
        }
    }

    return points;
}