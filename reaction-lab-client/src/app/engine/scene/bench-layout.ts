import { Box3, Color, Vector3 } from "three";
import { Phase } from "../resources/material-cache";

export interface UnitAtom {
    readonly symbol: string;
    readonly phase: Phase;
    readonly color: Color;
    readonly radius: number;
    readonly position: Vector3;
}

export interface LayoutUnit {
    readonly id: string;
    readonly substanceId: string;
    readonly atoms: readonly UnitAtom[];
}

export interface PlacedAtom extends UnitAtom {
    readonly unit: string;
    readonly substanceId: string;
}

export interface BenchLayout {
    readonly atoms: readonly PlacedAtom[];
    readonly bounds: Box3;
}

interface Measure {
    readonly center: Vector3;
    readonly radius: number;
    readonly floor: number;
}

const GAP = 1.6;

export function layoutBench(units: readonly LayoutUnit[]): BenchLayout {
    const atoms: PlacedAtom[] = [];
    const bounds = new Box3();
    let cursor = 0;

    for (const unit of units) {
        const { center, radius, floor } = measure(unit);
        const offset = new Vector3(cursor + radius - center.x, -floor, -center.z);

        for (const atom of unit.atoms) {
            const position = atom.position.clone().add(offset);

            atoms.push({ ...atom, position, unit: unit.id, substanceId: unit.substanceId });
            bounds.expandByPoint(position.clone().addScalar(atom.radius));
            bounds.expandByPoint(position.clone().subScalar(atom.radius));
        }

        cursor += radius * 2 + GAP;
    }

    recenter(atoms, bounds, (cursor - GAP) / 2);

    return { atoms, bounds };
}

export function ringPositions(radii: readonly number[]): Vector3[] {
    const count = radii.length;

    if (count <= 1) {
        return radii.map(() => new Vector3());
    }

    const mean = radii.reduce((sum, radius) => sum + radius, 0) / count;
    const ring = count === 2 ? mean : mean / Math.sin(Math.PI / count);

    return radii.map((_, index) => {
        const angle = (index / count) * Math.PI * 2;

        return new Vector3(Math.cos(angle) * ring, Math.sin(angle) * ring, 0);
    });
}

function measure(unit: LayoutUnit): Measure {
    const center = new Vector3();
    let radius = 0;
    let floor = Infinity;

    for (const atom of unit.atoms) {
        center.add(atom.position);
    }

    center.divideScalar(Math.max(unit.atoms.length, 1));

    for (const atom of unit.atoms) {
        radius = Math.max(radius, atom.position.distanceTo(center) + atom.radius);
        floor = Math.min(floor, atom.position.y - atom.radius);
    }

    return { center, radius, floor: Number.isFinite(floor) ? floor : 0 };
}

function recenter(atoms: readonly PlacedAtom[], bounds: Box3, shift: number): void {
    if (bounds.isEmpty()) {
        return;
    }

    for (const atom of atoms) {
        atom.position.x -= shift;
    }

    bounds.min.x -= shift;
    bounds.max.x -= shift;
}