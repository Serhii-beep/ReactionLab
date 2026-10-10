import { Sphere, Vector3 } from "three";
import { BenchLayout, PlacedAtom, PlacedBond } from "../scene/bench-layout";

export interface StagedBench {
    readonly atoms: readonly PlacedAtom[];
    readonly bonds: readonly PlacedBond[];
    readonly latticeBonds: readonly PlacedBond[];
    readonly sphereByUnitId: ReadonlyMap<string, Sphere>;
}

export interface MovingPoint {
    readonly live: Vector3;
    readonly resting: Vector3;
    readonly atom: PlacedAtom | null;
}

export interface UnitTravelPlan {
    readonly fromShift: Vector3;
    readonly toShift: Vector3;
}

export interface UnitTravel extends UnitTravelPlan {
    readonly unitId: string;
    readonly points: readonly MovingPoint[];
}

export interface StagedSet extends StagedBench {
    readonly travels: readonly UnitTravel[];
}

export interface PosedPoints {
    readonly positionByAtom: ReadonlyMap<PlacedAtom, Vector3>;
}

export type TravelPlanFor = (unitId: string, sphere: Sphere) => UnitTravelPlan | null;

const SHIFT_SCRATCH = new Vector3();

export function stageTravelingUnits(layout: BenchLayout, planFor: TravelPlanFor): StagedSet {
    const travels: UnitTravel[] = [];

    for (const [unitId, sphere] of layout.sphereByUnitId) {
        const plan = planFor(unitId, sphere);

        if (plan !== null) {
            travels.push({ ...plan, unitId, points: movingPointsOf(layout, unitId, sphere) });
        }
    }

    return { atoms: layout.atoms, bonds: layout.bonds, latticeBonds: layout.latticeBonds, sphereByUnitId: layout.sphereByUnitId, travels };
}

export function poseTravels(travels: readonly UnitTravel[], travelProgress: number): void {
    for (const travel of travels) {
        SHIFT_SCRATCH.lerpVectors(travel.fromShift, travel.toShift, travelProgress);

        for (const point of travel.points) {
            point.live.copy(point.resting).add(SHIFT_SCRATCH);
        }
    }
}

export function pointsAtPose(stagedSet: StagedSet, travelProgress: number): PosedPoints {
    const positionByAtom = new Map<PlacedAtom, Vector3>();

    for (const travel of stagedSet.travels) {
        const shift = new Vector3().lerpVectors(travel.fromShift, travel.toShift, travelProgress);

        for (const point of travel.points) {
            if (point.atom !== null) {
                positionByAtom.set(point.atom, point.resting.clone().add(shift));
            }
        }
    }

    return { positionByAtom };
}

export function mergePosedPoints(first: PosedPoints, second: PosedPoints): PosedPoints {
    return { positionByAtom: new Map([...first.positionByAtom, ...second.positionByAtom]) };
}

export function posedPositionOf(posed: PosedPoints, atom: PlacedAtom): Vector3 {
    const position = posed.positionByAtom.get(atom);

    if (position === undefined) {
        throw new Error(`No posed position for ${atom.symbol} of unit ${atom.unitId}`);
    }

    return position;
}

export function atomsOfUnits(bench: StagedBench, unitIds: ReadonlySet<string>): PlacedAtom[] {
    return bench.atoms.filter((atom) => unitIds.has(atom.unitId));
}

export function bondsOfUnits(bench: StagedBench, unitIds: ReadonlySet<string>): PlacedBond[] {
    return bench.bonds.filter((bond) => unitIds.has(bond.from.unitId) || unitIds.has(bond.to.unitId));
}

function movingPointsOf(layout: BenchLayout, unitId: string, sphere: Sphere): MovingPoint[] {
    const points: MovingPoint[] = [{ live: sphere.center, resting: sphere.center.clone(), atom: null }];

    for (const atom of layout.atoms) {
        if (atom.unitId === unitId) {
            points.push({ live: atom.position, resting: atom.position.clone(), atom });
        }
    }

    for (const bond of layout.bonds) {
        if (bond.from.unitId === unitId) {
            points.push({ live: bond.sidePoint, resting: bond.sidePoint.clone(), atom: null });
        }
    }

    return points;
}