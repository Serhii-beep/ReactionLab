import { Quaternion, Vector3 } from "three";
import { bestFitRotation } from "../../../engine/core/best-fit-rotation";
import { MoleculeSketch } from "../../../data/molecules/molecule-sketch";
import { PointGroupDescription, SketchDescription } from "../../../core/chemistry/chemistry-contracts";

export interface PlacedAxis {
    readonly direction: Vector3;
    readonly order: number;
}

export interface PlacedPointGroup {
    readonly schoenflies: string;
    readonly center: Vector3;
    readonly axes: readonly PlacedAxis[];
    readonly planeNormals: readonly Vector3[];
    readonly inversionCenter: boolean;
}

export interface PlacedSketch {
    readonly sketch: MoleculeSketch;
    readonly description: SketchDescription;
    readonly positions: readonly Vector3[];
    readonly openValencePositions: readonly Vector3[];
    readonly openValenceOwners: readonly number[];
    readonly pointGroup: PlacedPointGroup | null;
}

interface Alignment {
    readonly rotation: Quaternion;
    readonly from: Vector3;
    readonly onto: Vector3;
}

const FALLBACK_SPACING = 1.4;

export function placeSketch(sketch: MoleculeSketch, description: SketchDescription, previous: PlacedSketch | null): PlacedSketch {
    const computed = vectorsOf(description.positionsAngstroms);

    if (computed.length !== sketch.atoms.length) {
        return { sketch, description, positions: fallbackPositions(sketch, previous), openValencePositions: [], openValenceOwners: [], pointGroup: null };
    }

    const alignment = alignmentOnto(sketch, computed, previous);
    const place = (position: Vector3) => position.clone().sub(alignment.from).applyQuaternion(alignment.rotation).add(alignment.onto);

    return {
        sketch,
        description,
        positions: computed.map(place),
        openValencePositions: vectorsOf(description.openValencePositionsAngstroms).map(place),
        openValenceOwners: description.openValenceOwners ?? [],
        pointGroup: description.pointGroup ? placedPointGroup(description.pointGroup, alignment, place) : null
    };
}

export function vectorsOf(flattened: readonly number[] | undefined): Vector3[] {
    const coordinates = flattened ?? [];
    const vectors: Vector3[] = [];

    for (let index = 0; index + 2 < coordinates.length; index += 3) {
        vectors.push(new Vector3(coordinates[index], coordinates[index + 1], coordinates[index + 2]));
    }

    return vectors;
}

function alignmentOnto(sketch: MoleculeSketch, computed: readonly Vector3[], previous: PlacedSketch | null): Alignment {
    const previousIndexByKey = new Map(previous?.sketch.atoms.map((atom, index) => [atom.key, index]) ?? []);
    const shared = sketch.atoms
        .map((atom, index) => ({ index, previousIndex: previousIndexByKey.get(atom.key) }))
        .filter((pair): pair is { index: number; previousIndex: number } =>
            pair.previousIndex !== undefined && previous !== null && pair.previousIndex < previous.positions.length);

    if (previous === null || shared.length === 0) {
        return { rotation: new Quaternion(), from: centroidOf(computed), onto: new Vector3() };
    }

    const from = centroidOf(shared.map((pair) => computed[pair.index]));
    const onto = centroidOf(shared.map((pair) => previous.positions[pair.previousIndex]));
    const rotation = shared.length < 2
        ? new Quaternion()
        : bestFitRotation(
            shared.map((pair) => computed[pair.index].clone().sub(from)),
            shared.map((pair) => previous.positions[pair.previousIndex].clone().sub(onto)),
            new Quaternion());

    return { rotation, from, onto };
}

function placedPointGroup(
    group: PointGroupDescription,
    alignment: Alignment,
    place: (position: Vector3) => Vector3
): PlacedPointGroup {
    const turned = (components: readonly number[]) =>
        new Vector3(components[0], components[1], components[2]).applyQuaternion(alignment.rotation);

    return {
        schoenflies: group.schoenflies,
        center: place(new Vector3(group.centerAngstroms[0], group.centerAngstroms[1], group.centerAngstroms[2])),
        axes: group.rotationAxes.map((axis) => ({ direction: turned(axis.direction), order: axis.order })),
        planeNormals: group.mirrorPlanes.map((plane) => turned(plane.normal)),
        inversionCenter: group.hasInversionCenter
    };
}

function fallbackPositions(sketch: MoleculeSketch, previous: PlacedSketch | null): Vector3[] {
    const previousByKey = new Map<number, Vector3>();
    const positions: Vector3[] = [];

    if (previous !== null) {
        previous.sketch.atoms.forEach((atom, index) => previousByKey.set(atom.key, previous.positions[index]));
    }

    sketch.atoms.forEach((atom, index) => {
        const known = previousByKey.get(atom.key);
        const neighbor = sketch.bonds.find((bond) => bond.to === index && bond.from < index)?.from;

        positions.push(known?.clone()
            ?? (neighbor === undefined
                ? new Vector3(index * FALLBACK_SPACING, 0, 0)
                : positions[neighbor].clone().add(new Vector3(FALLBACK_SPACING, 0, 0).applyAxisAngle(new Vector3(0, 0, 1), index))));
    });

    return positions;
}

function centroidOf(positions: readonly Vector3[]): Vector3 {
    const sum = new Vector3();

    for (const position of positions) {
        sum.add(position);
    }

    return positions.length === 0 ? sum : sum.divideScalar(positions.length);
}
