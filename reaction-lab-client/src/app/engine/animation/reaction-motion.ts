import { Box3, Sphere, Vector3 } from "three";
import { BenchLayout, packUnits, PlacedAtom, PlacedBond } from "../scene/bench-layout";
import { easeInOutCubic, easeOutCubic, progressBetween } from "./easing";
import { ReactionScript } from "./reaction-script";

export interface StagedBench {
    readonly atoms: readonly PlacedAtom[];
    readonly bonds: readonly PlacedBond[];
    readonly sphereByUnitId: ReadonlyMap<string, Sphere>;
}

interface MovingPoint {
    readonly live: Vector3;
    readonly resting: Vector3;
}

interface UnitTravel {
    readonly points: readonly MovingPoint[];
    readonly fromShift: Vector3;
    readonly toShift: Vector3;
}

interface StagedSet extends StagedBench {
    readonly travels: readonly UnitTravel[];
}

type ShiftsFor = (unitId: string, sphere: Sphere) => readonly [Vector3, Vector3] | null;

export class ReactionMotion {
    readonly bounds: Box3;

    private readonly reactantsSet: StagedSet;
    private readonly productsSet: StagedSet;
    private readonly shift = new Vector3();

    constructor(private readonly script: ReactionScript, before: BenchLayout, after: BenchLayout) {
        const consumedIds = [...before.sphereByUnitId.keys()].filter((unitId) => !after.sphereByUnitId.has(unitId));
        const producedIds = [...after.sphereByUnitId.keys()].filter((unitId) => !before.sphereByUnitId.has(unitId));
        const meeting = meetingPointOf(before, consumedIds);
        const gatheredReactants = gatheredCentersOf(before, consumedIds, meeting);
        const gatheredProducts = gatheredCentersOf(after, producedIds, meeting);

        this.bounds = before.bounds.clone().union(after.bounds);
        this.reactantsSet = stage(before, (unitId, sphere) => {
            const gathered = gatheredReactants.get(unitId);

            return gathered ? [new Vector3(), gathered.clone().sub(sphere.center)] : null;
        });
        this.productsSet = stage(after, (unitId, sphere) => {
            const gathered = gatheredProducts.get(unitId);

            if (gathered) {
                return [gathered.clone().sub(sphere.center), new Vector3()];
            }

            const restingBefore = before.sphereByUnitId.get(unitId);

            return restingBefore ? [restingBefore.center.clone().sub(sphere.center), new Vector3()] : null;
        });
    }

    frameAt(seconds: number): StagedBench {
        const { cues, durationSeconds } = this.script;

        if (seconds < cues.swapSeconds) {
            return this.pose(this.reactantsSet, easeInOutCubic(progressBetween(0, cues.gatheredSeconds, seconds)));
        }

        return this.pose(this.productsSet, easeOutCubic(progressBetween(cues.releaseSeconds, durationSeconds, seconds)));
    }

    private pose(set: StagedSet, progress: number): StagedBench {
        for (const travel of set.travels) {
            this.shift.lerpVectors(travel.fromShift, travel.toShift, progress);

            for (const point of travel.points) {
                point.live.copy(point.resting).add(this.shift);
            }
        }

        return set;
    }
}

function stage(layout: BenchLayout, shiftsFor: ShiftsFor): StagedSet {
    const travels: UnitTravel[] = [];

    for (const [unitId, sphere] of layout.sphereByUnitId) {
        const shifts = shiftsFor(unitId, sphere);

        if (shifts !== null) {
            travels.push({ points: movingPointsOf(layout, unitId, sphere), fromShift: shifts[0], toShift: shifts[1] });
        }
    }

    return { atoms: layout.atoms, bonds: layout.bonds, sphereByUnitId: layout.sphereByUnitId, travels };
}

function movingPointsOf(layout: BenchLayout, unitId: string, sphere: Sphere): MovingPoint[] {
    const points: MovingPoint[] = [{ live: sphere.center, resting: sphere.center.clone() }];
    const centroids = new Set<Vector3>();

    for (const atom of layout.atoms) {
        if (atom.unitId === unitId) {
            points.push({ live: atom.position, resting: atom.position.clone() });
        }
    }

    for (const bond of layout.bonds) {
        if (bond.from.unitId === unitId) {
            centroids.add(bond.centroid);
        }
    }

    for (const centroid of centroids) {
        points.push({ live: centroid, resting: centroid.clone() });
    }

    return points;
}

function meetingPointOf(layout: BenchLayout, unitIds: readonly string[]): Vector3 {
    const meeting = new Vector3();

    for (const unitId of unitIds) {
        const sphere = layout.sphereByUnitId.get(unitId);

        if (sphere) {
            meeting.add(sphere.center);
        }
    }

    meeting.divideScalar(Math.max(unitIds.length, 1));
    meeting.y = 0;

    return meeting;
}

function gatheredCentersOf(layout: BenchLayout, unitIds: readonly string[], meeting: Vector3): ReadonlyMap<string, Vector3> {
    const spheres = unitIds.flatMap((unitId) => {
        const sphere = layout.sphereByUnitId.get(unitId);

        return sphere ? [{ unitId, sphere }] : [];
    });

    const packed = packUnits(spheres.map(({ sphere }) => sphere.radius), 0);
    const centers = new Map<string, Vector3>();

    spheres.forEach(({ unitId, sphere }, index) => {
        const slot = packed.centers[index];

        centers.set(unitId, new Vector3(meeting.x + slot.x, sphere.center.y, meeting.z + slot.z));
    });

    return centers;
}