import { Box3, Color, Sphere, Vector3 } from "three";
import { Phase, ROOM_TEMPERATURE_KELVIN } from "../core/matter";
import { latticeContactsOf } from "./lattice-contacts";

export type BondKind = 'single' | 'double' | 'triple' | 'aromatic' | 'ionic' | 'hydrogen' | 'metallic';

export interface IonMembership {
    readonly index: number;
    readonly charge: number;
}

export interface UnitAtom {
    readonly symbol: string;
    readonly phase: Phase;
    readonly color: Color;
    readonly radius: number;
    readonly position: Vector3;
    readonly ion: IonMembership | null;
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
    readonly crystallite: boolean;
}

export interface PlacedAtom extends UnitAtom {
    readonly unitId: string;
    readonly substanceId: string;
    radius: number;
    temperatureKelvin: number;
}

export interface PlacedBond {
    readonly from: PlacedAtom;
    readonly to: PlacedAtom;
    readonly kind: BondKind;
    readonly sidePoint: Vector3;
    strength: number;
    partial: boolean;
}

export interface BenchLayout {
    readonly atoms: readonly PlacedAtom[];
    readonly bonds: readonly PlacedBond[];
    readonly latticeBonds: readonly PlacedBond[];
    readonly bounds: Box3;
    readonly sphereByUnitId: ReadonlyMap<string, Sphere>;
}

interface PackedUnits {
    readonly centers: readonly Vector3[];
    readonly width: number;
    readonly depth: number;
}

interface UnitExtent {
    readonly center: Vector3;
    readonly radius: number;
    readonly floor: number;
}

interface UnitBlock {
    readonly start: number;
    readonly end: number;
}

interface PlacementGroup {
    readonly extent: UnitExtent;
    readonly unitIndices: readonly number[];
}

interface BenchUnderConstruction {
    readonly atoms: PlacedAtom[];
    readonly bonds: PlacedBond[];
    readonly latticeBonds: PlacedBond[];
    readonly bounds: Box3;
    readonly sphereByUnitId: Map<string, Sphere>;
}

const GAP = 1.6;
const ROW_LIMIT = 8;
const RING_SPREAD = 1.85;
export const FLOOR_CLEARANCE = 0.12;

export function layoutBench(units: readonly LayoutUnit[]): BenchLayout {
    const extents = units.map((unit) => measureAtoms(unit.atoms));
    const groupsByBlock = blocksBySubstance(units).map((block) => placementGroupsOf(units, extents, block));
    const packs = groupsByBlock.map((groups) => packUnits(groups.map((group) => group.extent.radius), GAP));
    const benchWidth = packs.reduce((sum, pack) => sum + pack.width, 0) + GAP * Math.max(packs.length - 1, 0);
    const bench: BenchUnderConstruction = { atoms: [], bonds: [], latticeBonds: [], bounds: new Box3(), sphereByUnitId: new Map() };
    let blockStart = -benchWidth / 2;

    groupsByBlock.forEach((groups, blockIndex) => {
        const pack = packs[blockIndex];

        groups.forEach((group, groupIndex) => {
            const slot = pack.centers[groupIndex];
            const offset = new Vector3(blockStart + pack.width / 2 + slot.x - group.extent.center.x, FLOOR_CLEARANCE - group.extent.floor, slot.z - group.extent.center.z);
            const firstAtom = bench.atoms.length;
            const firstBond = bench.bonds.length;

            for (const index of group.unitIndices) {
                placeUnits(units[index], extents[index], offset, bench);
            }

            if (units[group.unitIndices[0]].crystallite) {
                bench.latticeBonds.push(...latticeContactsOf(bench.atoms.slice(firstAtom), bench.bonds.slice(firstBond)));
            }
        });

        blockStart += pack.width + GAP;
    });

    return bench;
}

export function smallestRadiusOf(atoms: readonly PlacedAtom[]): number {
    let smallest = Infinity;

    for (const atom of atoms) {
        if (atom.radius > 0) {
            smallest = Math.min(smallest, atom.radius);
        }
    }

    return smallest;
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

function packUnits(radii: readonly number[], gap: number): PackedUnits {
    const rows: number[][] = [];

    for (let index = 0; index < radii.length; index += ROW_LIMIT) {
        rows.push(radii.slice(index, index + ROW_LIMIT));
    }

    const rowWidths = rows.map((row) => row.reduce((sum, radius) => sum + radius * 2, 0) + gap * (row.length - 1));
    const rowDepths = rows.map((row) => Math.max(...row) * 2);
    const depth = rowDepths.reduce((sum, rowDepth) => sum + rowDepth, 0) + gap * Math.max(rows.length - 1, 0);
    const centers: Vector3[] = [];
    let rowStart = -depth / 2;

    rows.forEach((row, rowIndex) => {
        let cursor = -rowWidths[rowIndex] / 2;

        for (const radius of row) {
            centers.push(new Vector3(cursor + radius, 0, rowStart + rowDepths[rowIndex] / 2));
            cursor += radius * 2 + gap;
        }

        rowStart += rowDepths[rowIndex] + gap;
    });

    return { centers, width: Math.max(0, ...rowWidths), depth };
}

function blocksBySubstance(units: readonly LayoutUnit[]): UnitBlock[] {
    const blocks: UnitBlock[] = [];

    units.forEach((unit, index) => {
        const last = blocks[blocks.length - 1];

        if (last !== undefined && units[last.start].substanceId === unit.substanceId) {
            blocks[blocks.length - 1] = { start: last.start, end: index + 1 };
        } else {
            blocks.push({ start: index, end: index + 1 });
        }
    });

    return blocks;
}

function placementGroupsOf(units: readonly LayoutUnit[], extents: readonly UnitExtent[], block: UnitBlock): PlacementGroup[] {
    const unitIndices = Array.from({ length: block.end - block.start }, (_, index) => block.start + index);

    if (units[block.start].crystallite) {
        return [{ extent: measureAtoms(unitIndices.flatMap((index) => units[index].atoms)), unitIndices }];
    }

    return unitIndices.map((index) => ({ extent: extents[index], unitIndices: [index] }));
}

function placeUnits(unit: LayoutUnit, extent: UnitExtent, offset: Vector3, bench: BenchUnderConstruction): void {
    const placed = unit.atoms.map((atom) => place(atom, unit, offset));
    const centroid = extent.center.clone().add(offset);

    for (const atom of placed) {
        bench.atoms.push(atom);
        bench.bounds.expandByPoint(atom.position.clone().addScalar(atom.radius));
        bench.bounds.expandByPoint(atom.position.clone().subScalar(atom.radius));
    }

    for (const bond of unit.bonds) {
        bench.bonds.push({ from: placed[bond.from], to: placed[bond.to], kind: bond.kind, sidePoint: centroid.clone(), strength: 1, partial: false });
    }

    bench.sphereByUnitId.set(unit.id, new Sphere(centroid.clone(), extent.radius));
}

function place(atom: UnitAtom, unit: LayoutUnit, offset: Vector3): PlacedAtom {
    return { ...atom, position: atom.position.clone().add(offset), unitId: unit.id, substanceId: unit.substanceId, temperatureKelvin: ROOM_TEMPERATURE_KELVIN };
}

function measureAtoms(atoms: readonly UnitAtom[]): UnitExtent {
    const center = new Vector3();
    let radius = 0;
    let floor = Infinity;

    for (const atom of atoms) {
        center.add(atom.position);
    }

    center.divideScalar(Math.max(atoms.length, 1));

    for (const atom of atoms) {
        radius = Math.max(radius, atom.position.distanceTo(center) + atom.radius);
        floor = Math.min(floor, atom.position.y - atom.radius);
    }

    return { center, radius, floor: Number.isFinite(floor) ? floor : 0 };
}