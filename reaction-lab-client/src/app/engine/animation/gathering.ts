import { Box3, Sphere, Vector3 } from "three";
import { BenchLayout } from "../scene/bench-layout";

export interface Gathering {
    readonly meeting: Vector3;
    readonly centerByUnitId: CentersByUnitId;
}

export type CentersByUnitId = ReadonlyMap<string, Vector3>;

interface BenchSpot {
    readonly unitId: string;
    readonly sphere: Sphere;
}

interface GatheringSlot {
    readonly unitId: string;
    readonly radius: number;
    readonly offset: Vector3;
}

interface PackedSphere {
    readonly offset: Vector3;
    readonly radius: number;
}

interface BlockedStretch {
    readonly from: number;
    readonly to: number;
}

const PACKING = 0.72;
const RING_LIMIT = 6;
const RING_START_RADIANS = (7 * Math.PI) / 6;
const HOVER_CLEARANCE = 0.35;
const GOLDEN_ANGLE_RADIANS = Math.PI * (3 - Math.sqrt(5));
const PACKING_DIRECTIONS = fibonacciDirectionsOf(64);
const DIRECTION_SLACK = 0.3;
const MAX_TRADING_PASSES = 12;
const TRADING_TOLERANCE = 1e-6;
const WAY_SCRATCH = new Vector3();

export function gatheringOf(layout: BenchLayout, unitIds: readonly string[]): Gathering {
    const spots = benchSpotsOf(layout, unitIds);
    const meeting = meetingFloorOf(layout, spots);

    meeting.y = hoverHeightOf(slotsFor(spots, meeting));

    const slots = slotsFor(spots, meeting);

    meeting.y = hoverHeightOf(slots);

    return { meeting, centerByUnitId: centersAround(meeting, slots) };
}

export function slotCentersAround(meeting: Vector3, layout: BenchLayout, unitIds: readonly string[]): CentersByUnitId {
    return centersAround(meeting, slotsFor(benchSpotsOf(layout, unitIds), meeting));
}

export function boundsWithGathering(before: BenchLayout, after: BenchLayout, gathering: Gathering): Box3 {
    const bounds = before.bounds.clone().union(after.bounds);
    const gatheredBounds = new Box3();

    for (const [unitId, center] of gathering.centerByUnitId) {
        const radius = before.sphereByUnitId.get(unitId)?.radius ?? 0;

        bounds.union(new Sphere(center, radius).getBoundingBox(gatheredBounds));
    }

    return bounds;
}

function benchSpotsOf(layout: BenchLayout, unitIds: readonly string[]): BenchSpot[] {
    return unitIds.flatMap((unitId) => {
        const sphere = layout.sphereByUnitId.get(unitId);

        return sphere ? [{ unitId, sphere }] : [];
    })
    .sort((first, second) => first.sphere.center.x - second.sphere.center.x);
}

function slotsFor(spots: readonly BenchSpot[], meeting: Vector3): GatheringSlot[] {
    const ringed = spots.length <= RING_LIMIT;
    const offsets = ringed ? ringOffsetsOf(spots) : packedOffsetsOf(spots, meeting);
    let pass = 0;

    while (pass < MAX_TRADING_PASSES && tradingPass(spots, offsets, meeting, !ringed)) {
        pass++;
    }

    return spots.map((spot, index) => ({ unitId: spot.unitId, radius: spot.sphere.radius, offset: offsets[index] }));
}

function ringOffsetsOf(spots: readonly BenchSpot[]): Vector3[] {
    const count = spots.length;

    if (count <= 1) {
        return spots.map(() => new Vector3());
    }

    const [largest, secondLargest] = spots.map(({ sphere }) => sphere.radius).sort((first, second) => second - first);
    const ringRadius = (PACKING * (largest + secondLargest)) / (2 * Math.sin(Math.PI / count));

    return spots.map((_, index) => {
        const angle = count === 2 ? Math.PI * (1 - index) : RING_START_RADIANS + (index * 2 * Math.PI) / count;

        return new Vector3(Math.cos(angle) * ringRadius, Math.sin(angle) * ringRadius, 0);
    })
    .sort((first, second) => first.x - second.x);
}

function packedOffsetsOf(spots: readonly BenchSpot[], meeting: Vector3): Vector3[] {
    const order = spots.map((_, index) => index).sort((first, second) => spots[second].sphere.radius - spots[first].sphere.radius || first - second);
    const offsets = spots.map(() => new Vector3());
    const placed: PackedSphere[] = [];

    for (const index of order) {
        const { center, radius } = spots[index].sphere;
        const homeward = center.clone().sub(meeting).setY(0).normalize();

        offsets[index] = placed.length === 0 ? new Vector3() : nearestFitOf(radius, homeward, placed);
        placed.push({ offset: offsets[index], radius });
    }

    return offsets;
}

function nearestFitOf(radius: number, homeward: Vector3, placed: readonly PackedSphere[]): Vector3 {
    const reaches = PACKING_DIRECTIONS.map((direction) => nearestReachAlong(direction, radius, placed));
    const nearest = Math.min(...reaches);
    let chosen = 0;
    let bestAlignment = -Infinity;

    PACKING_DIRECTIONS.forEach((direction, index) => {
        const alignment = direction.dot(homeward);

        if (reaches[index] <= nearest + DIRECTION_SLACK && alignment > bestAlignment) {
            bestAlignment = alignment;
            chosen = index;
        }
    });

    return PACKING_DIRECTIONS[chosen].clone().multiplyScalar(reaches[chosen]);
}

function nearestReachAlong(direction: Vector3, radius: number, placed: readonly PackedSphere[]): number {
    const blocked = placed.flatMap((sphere) => blockedStretchOf(direction, radius, sphere)).sort((first, second) => first.from - second.from);
    let reach = 0;

    for (const stretch of blocked) {
        if (stretch.from > reach) {
            break;
        }

        reach = Math.max(reach, stretch.to);
    }

    return reach;
}

function blockedStretchOf(direction: Vector3, radius: number, sphere: PackedSphere): BlockedStretch[] {
    const gap = PACKING * (radius + sphere.radius);
    const along = direction.dot(sphere.offset);
    const discriminant = along * along - sphere.offset.lengthSq() + gap * gap;

    if (discriminant <= 0) {
        return [];
    }

    const halfWidth = Math.sqrt(discriminant);

    return along + halfWidth <= 0 ? [] : [{ from: along - halfWidth, to: along + halfWidth }];
}

function tradingPass(spots: readonly BenchSpot[], offsets: Vector3[], meeting: Vector3, sameSizeOnly: boolean): boolean {
    let traded = false;

    for (let first = 0; first < spots.length; first++) {
        for (let second = first + 1; second < spots.length; second++) {
            const sameSize = spots[first].sphere.radius === spots[second].sphere.radius;

            traded = ((!sameSizeOnly || sameSize) && tradeIfShorter(spots, offsets, meeting, first, second)) || traded;
        }
    }

    return traded;
}

function tradeIfShorter(spots: readonly BenchSpot[], offsets: Vector3[], meeting: Vector3, first: number, second: number): boolean {
    const kept = wayLength(spots[first], offsets[first], meeting) + wayLength(spots[second], offsets[second], meeting);
    const traded = wayLength(spots[first], offsets[second], meeting) + wayLength(spots[second], offsets[first], meeting);

    if (traded >= kept - TRADING_TOLERANCE) {
        return false;
    }

    [offsets[first], offsets[second]] = [offsets[second], offsets[first]];

    return true;
}

function wayLength(spot: BenchSpot, offset: Vector3, meeting: Vector3): number {
    return WAY_SCRATCH.copy(meeting).add(offset).distanceTo(spot.sphere.center);
}

function hoverHeightOf(slots: readonly GatheringSlot[]): number {
    return slots.reduce((height, slot) => Math.max(height, slot.radius + HOVER_CLEARANCE - slot.offset.y), 0);
}

function centersAround(meeting: Vector3, slots: readonly GatheringSlot[]): CentersByUnitId {
    return new Map(slots.map(({ unitId, offset }) => [unitId, meeting.clone().add(offset)]));
}

function meetingFloorOf(layout: BenchLayout, spots: readonly BenchSpot[]): Vector3 {
    if (spots.length === 0) {
        return layout.bounds.getCenter(new Vector3()).setY(0);
    }

    return spots.reduce((sum, { sphere }) => sum.add(sphere.center), new Vector3()).divideScalar(spots.length).setY(0);
}

function fibonacciDirectionsOf(count: number): Vector3[] {
    return Array.from({ length: count }, (_, index) => {
        const height = 1 - (2 * (index + 0.5)) / count;
        const across = Math.sqrt(1 - height * height);
        const angle = index * GOLDEN_ANGLE_RADIANS;

        return new Vector3(Math.cos(angle) * across, height, Math.sin(angle) * across);
    });
}