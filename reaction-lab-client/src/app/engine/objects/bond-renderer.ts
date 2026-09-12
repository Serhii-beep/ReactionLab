import { BufferGeometry, Group, Vector3 } from "three";
import { BondKind, PlacedAtom, PlacedBond } from "../scene/bench-layout";
import { MaterialCache } from "../resources/material-cache";
import { Disposable } from "../core/disposal-scope";
import { BatchRequest, commit, InstancedBatches } from "./instanced-batches";
import { GeometryCache, Lod } from "../resources/geometry-cache";
import { CylinderTransform } from "./cylinder-transform";


type Stroke = 'solid' | 'dashed' | 'dotted';

interface Line {
    readonly offset: number;
    readonly radius: number;
    readonly stroke: Stroke;
}

interface Pattern {
    readonly period: number;
    readonly duty: number;
}

interface BondPiece {
    readonly atom: PlacedAtom;
    readonly start: Vector3;
    readonly end: Vector3;
    readonly radius: number;
}

type BondPieces = Map<string, BondPiece[]>;

const LINES: Readonly<Record<BondKind, readonly Line[]>> = {
    single: [{ offset: 0, radius: 0.08, stroke: 'solid' }],
    double: [{ offset: -0.11, radius: 0.055, stroke: 'solid' }, { offset: 0.11, radius: 0.055, stroke: 'solid' }],
    triple: [
        { offset: -0.15, radius: 0.048, stroke: 'solid' },
        { offset: 0, radius: 0.048, stroke: 'solid' },
        { offset: 0.15, radius: 0.048, stroke: 'solid' }
    ],
    aromatic: [{ offset: 0, radius: 0.065, stroke: 'solid' }, { offset: 0.15, radius: 0.04, stroke: 'dashed' }],
    ionic: [{ offset: 0, radius: 0.06, stroke: 'dashed' }],
    hydrogen: [{ offset: 0, radius: 0.045, stroke: 'dotted' }],
    metallic: [{ offset: 0, radius: 0.08, stroke: 'solid' }]
};

const PATTERNS: Readonly<Record<Exclude<Stroke, 'solid'>, Pattern>> = {
    dashed: { period: 0.3, duty: 0.6 },
    dotted: { period: 0.16, duty: 0 }
};

const UP = new Vector3(0, 1, 0);
const RIGHT = new Vector3(1, 0, 0);

export function bondExtent(kind: BondKind): number {
    return Math.max(...LINES[kind].map((line) => Math.abs(line.offset) + line.radius));
}

export class BondRenderer implements Disposable {
    readonly root = new Group();

    private readonly lines = new InstancedBatches('bond-lines');
    private readonly dashes = new InstancedBatches('bond-dashes');
    private readonly dots = new InstancedBatches('bond-dots');
    private readonly axis = new Vector3();
    private readonly center = new Vector3();
    private readonly perpendicular = new Vector3();
    private readonly cylinder = new CylinderTransform();

    constructor(
        private readonly geometries: GeometryCache,
        private readonly materials: MaterialCache
    ) {
        this.root.name = 'bonds';
        this.root.add(this.lines.root, this.dashes.root, this.dots.root);
    }

    render(bonds: readonly PlacedBond[], lod: Lod): void {
        const pieces: Record<Stroke, BondPieces> = { solid: new Map(), dashed: new Map(), dotted: new Map() };

        for (const bond of bonds) {
            const perpendicular = this.perpendicularOf(bond).clone();

            for (const line of LINES[bond.kind]) {
                const shift = perpendicular.clone().multiplyScalar(line.offset);
                const made = line.stroke === 'solid' ? halves(bond, line, shift) : patterned(bond, line, shift, PATTERNS[line.stroke]);

                for (const piece of made) {
                    collect(pieces[line.stroke], piece);
                }
            }
        }

        this.draw(this.lines, this.geometries.cylinder(lod), pieces.solid);
        this.draw(this.dashes, this.geometries.dash(lod), pieces.dashed);
        this.draw(this.dots, this.geometries.sphere(lod), pieces.dotted);
    }

    dispose(): void {
        this.lines.dispose();
        this.dashes.dispose();
        this.dots.dispose();
        this.root.removeFromParent();
    }

    private draw(batches: InstancedBatches, geometry: BufferGeometry, groups: BondPieces): void {
        const requests = new Map<string, BatchRequest>();

        for (const [key, [sample, ...rest]] of groups) {
            const material = this.materials.atom(sample.atom.symbol, sample.atom.phase, sample.atom.color);

            requests.set(key, { material, count: rest.length + 1, castShadow: true });
        }

        const meshes = batches.sync(geometry, requests);

        for (const [key, group] of groups) {
            const mesh = meshes.get(key);

            if (!mesh) {
                continue;
            }

            group.forEach((piece, index) => mesh.setMatrixAt(index, this.cylinder.between(piece.start, piece.end, piece.radius)));
            commit(mesh);
        }
    }

    private perpendicularOf(bond: PlacedBond): Vector3 {
        this.axis.copy(bond.to.position).sub(bond.from.position).normalize();
        this.center.copy(bond.from.position).add(bond.to.position).multiplyScalar(0.5);
        this.perpendicular.copy(bond.centroid).sub(this.center);
        this.perpendicular.addScaledVector(this.axis, -this.perpendicular.dot(this.axis));

        if (this.perpendicular.lengthSq() < 1e-6) {
            this.perpendicular.crossVectors(this.axis, Math.abs(this.axis.y) < 0.9 ? UP : RIGHT);
        }

        return this.perpendicular.normalize();
    }
}

function collect(groups: BondPieces, piece: BondPiece): void {
    const key = `${piece.atom.symbol}|${piece.atom.phase}`;
    const group = groups.get(key);

    if (group) {
        group.push(piece);
    } else {
        groups.set(key, [piece]);
    }
}

function halves(bond: PlacedBond, line: Line, shift: Vector3): BondPiece[] {
    const middle = bond.from.position.clone().add(bond.to.position).multiplyScalar(0.5).add(shift);

    return [
        { atom: bond.from, start: bond.from.position.clone().add(shift), end: middle, radius: line.radius },
        { atom: bond.to, start: bond.to.position.clone().add(shift), end: middle.clone(), radius: line.radius }
    ];
}

function patterned(bond: PlacedBond, line: Line, shift: Vector3, pattern: Pattern): BondPiece[] {
    const axis = bond.to.position.clone().sub(bond.from.position);
    const distance = axis.length();
    const span = distance - bond.from.radius - bond.to.radius;

    if (span <= 0) {
        return [];
    }

    axis.divideScalar(distance);

    const count = Math.max(1, Math.round(span / pattern.period));
    const step = span / count;
    const half = axis.clone().multiplyScalar((pattern.duty * step) / 2);
    const pieces: BondPiece[] = [];

    for (let index = 0; index < count; index++) {
        const along = bond.from.radius + step * (index + 0.5);
        const center = bond.from.position.clone().addScaledVector(axis, along).add(shift);

        pieces.push({
            atom: along < distance / 2 ? bond.from : bond.to,
            start: center.clone().sub(half),
            end: center.add(half),
            radius: line.radius
        });
    }

    return pieces;
}