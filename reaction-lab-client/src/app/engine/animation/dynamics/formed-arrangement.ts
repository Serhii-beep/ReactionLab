import { Quaternion, Vector3 } from "three";
import { Molecule, SimulatedAtom } from "./molecular-system";
import { bestFitRotation } from "../../core/best-fit-rotation";

interface PlacedProduct {
    readonly molecule: Molecule;
    readonly center: Vector3;
    readonly shape: readonly Vector3[];
    readonly atomPositions: readonly Vector3[];
    readonly reach: number;
}

const CONTACT_SHARE = 1.05;
const NUDGE_SHARE = 0.55;
const NUDGE_ROUNDS = 30;
const OVERLAP_TOLERANCE = 0.01;

export function formedArrangementOf(current: readonly Vector3[], products: readonly Molecule[], productRest: Float64Array, atoms: readonly SimulatedAtom[]): Vector3[] {
    const placed = products.map((molecule) => placedProductOf(molecule, current, productRest, atoms));

    separateOverlapping(placed, atoms);

    const arrangement = current.map((offset) => offset.clone());

    for (const { molecule, atomPositions } of placed) {
        molecule.atomIndices.forEach((index, position) => arrangement[index].copy(atomPositions[position]));
    }

    const totalMass = atoms.reduce((sum, atom) => sum + atom.mass, 0);
    const massCenter = arrangement.reduce((sum, offset, index) => sum.addScaledVector(offset, atoms[index].mass / totalMass), new Vector3());

    return arrangement.map((offset) => offset.sub(massCenter));
}

function placedProductOf(molecule: Molecule, current: readonly Vector3[], productRest: Float64Array, atoms: readonly SimulatedAtom[]): PlacedProduct {
    const atomIndices = molecule.atomIndices;
    const center = meanOf(atomIndices.map((index) => current[index]));
    const restPoints = atomIndices.map((index) => new Vector3(productRest[index * 3], productRest[index * 3 + 1], productRest[index * 3 + 2]));
    const restCenter = meanOf(restPoints);
    const restShape = restPoints.map((point) => point.sub(restCenter));
    const turn = atomIndices.length > 1
        ? bestFitRotation(restShape, atomIndices.map((index) => current[index].clone().sub(center)), new Quaternion())
        : new Quaternion();
    const shape = restShape.map((offset) => offset.applyQuaternion(turn));
    const reach = atomIndices.reduce((farthest, index, position) => Math.max(farthest, shape[position].length() + atoms[index].radius * CONTACT_SHARE), 0);
    const placed = { molecule, center, shape, atomPositions: shape.map(() => new Vector3()), reach };

    placeAtoms(placed);

    return placed;
}

function separateOverlapping(placed: readonly PlacedProduct[], atoms: readonly SimulatedAtom[]): void {
    for (let round = 0; round < NUDGE_ROUNDS; round++) {
        let nudged = false;

        for (let first = 0; first < placed.length; first++) {
            for (let second = first + 1; second < placed.length; second++) {
                nudged = nudgeApart(placed[first], placed[second], atoms) || nudged;
            }
        }

        if (!nudged) {
            return;
        }
    }
}

function nudgeApart(first: PlacedProduct, second: PlacedProduct, atoms: readonly SimulatedAtom[]): boolean {
    const apart = first.center.clone().sub(second.center);

    if (apart.length() > first.reach + second.reach) {
        return false;
    }

    const overlap = deepestOverlapOf(first, second, atoms);

    if (overlap <= OVERLAP_TOLERANCE) {
        return false;
    }

    if (apart.lengthSq() < 1e-6) {
        apart.set(1, 0, 0);
    }

    apart.normalize().multiplyScalar(overlap * NUDGE_SHARE);
    first.center.add(apart);
    second.center.sub(apart);
    placeAtoms(first);
    placeAtoms(second);

    return true;
}

function deepestOverlapOf(first: PlacedProduct, second: PlacedProduct, atoms: readonly SimulatedAtom[]): number {
    let deepest = 0;

    first.molecule.atomIndices.forEach((firstIndex, firstPosition) => {
        second.molecule.atomIndices.forEach((secondIndex, secondPosition) => {
            const reach = (atoms[firstIndex].radius + atoms[secondIndex].radius) * CONTACT_SHARE;

            deepest = Math.max(deepest, reach - first.atomPositions[firstPosition].distanceTo(second.atomPositions[secondPosition]));
        });
    });

    return deepest;
}

function placeAtoms(product: PlacedProduct): void {
    product.shape.forEach((offset, position) => product.atomPositions[position].copy(product.center).add(offset));
}

function meanOf(points: readonly Vector3[]): Vector3 {
    return points.reduce((sum, point) => sum.add(point), new Vector3()).divideScalar(Math.max(points.length, 1));
}