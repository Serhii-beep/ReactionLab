import { Box3, Color, Sphere, Vector3 } from "three";
import { Phase } from "../core/matter";

export type BondKind = 'single' | 'double' | 'triple' | 'aromatic' | 'ionic' | 'hydrogen' | 'metallic';

export interface UnitAtom {
    readonly symbol: string;
    readonly phase: Phase;
    readonly color: Color;
    readonly radius: number;
    readonly position: Vector3;
}

export interface UnitBond {
    readonly from: number;
    readonly to: number;
    readonly kind: BondKind;
}

export interface LayoutUnit {
    readonly id: string;
    readonly substanceId: string;
    readonly atoms: readonly UnitAtom[];
    readonly bonds: readonly UnitBond[];
}

export interface PlacedAtom extends UnitAtom {
    readonly unitId: string;
    readonly substanceId: string;
}

export interface PlacedBond {
    readonly from: PlacedAtom;
    readonly to: PlacedAtom;
    readonly kind: BondKind;
    readonly centroid: Vector3;
}

export interface BenchLayout {
    readonly atoms: readonly PlacedAtom[];
    readonly bonds: readonly PlacedBond[];
    readonly bounds: Box3;
    readonly units: ReadonlyMap<string, Sphere>;
}

interface UnitExtent {
    readonly center: Vector3;
    readonly radius: number;
    readonly floor: number;
}

const GAP = 1.6;
const RING_SPREAD = 1.85;
const FLOOR_CLEARANCE = 0.12;

export function layoutBench(units: readonly LayoutUnit[]): BenchLayout {
    const measures = units.map(measureUnit);
    const width = measures.reduce((sum, item) => sum + item.radius * 2, 0) + GAP * Math.max(units.length - 1, 0);
    const atoms: PlacedAtom[] = [];
    const bonds: PlacedBond[] = [];
    const bounds = new Box3();
    const sphereByUnitId = new Map<string, Sphere>();
    let cursor = -width / 2;

    units.forEach((unit, index) => {
        const { center, radius, floor } = measures[index];
        const offset = new Vector3(cursor + radius - center.x, FLOOR_CLEARANCE -floor, -center.z);
        const placed = unit.atoms.map((atom) => place(atom, unit, offset));
        const centroid = center.clone().add(offset);

        for (const atom of placed) {
            atoms.push(atom);
            bounds.expandByPoint(atom.position.clone().addScalar(atom.radius));
            bounds.expandByPoint(atom.position.clone().subScalar(atom.radius));
        }

        for (const bond of unit.bonds) {
            bonds.push({ from: placed[bond.from], to: placed[bond.to], kind: bond.kind, centroid });
        }

        sphereByUnitId.set(unit.id, new Sphere(centroid.clone(), radius));
        cursor += radius * 2 + GAP;
    });

    return { atoms, bonds, bounds, units: sphereByUnitId };
}

export function ringPositions(radii: readonly number[]): Vector3[] {
    const count = radii.length;

    if (count <= 1) {
        return radii.map(() => new Vector3());
    }

    const mean = radii.reduce((sum, radius) => sum + radius, 0) / count;
    const ring = (count === 2 ? mean : mean / Math.sin(Math.PI / count)) * RING_SPREAD;

    return radii.map((_, index) => {
        const angle = (index / count) * Math.PI * 2;

        return new Vector3(Math.cos(angle) * ring, Math.sin(angle) * ring, 0);
    });
}

function place(atom: UnitAtom, unit: LayoutUnit, offset: Vector3): PlacedAtom {
    return { ...atom, position: atom.position.clone().add(offset), unitId: unit.id, substanceId: unit.substanceId };
}

function measureUnit(unit: LayoutUnit): UnitExtent {
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