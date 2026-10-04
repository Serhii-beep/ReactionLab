import { Vector3 } from "three";
import { BondKind, PlacedBond } from "./bench-layout";

export type Stroke = 'solid' | 'dashed' | 'dotted';

export interface Line {
    readonly offset: number;
    readonly radius: number;
    readonly stroke: Stroke;
}

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

const PARTIAL_LINES: readonly Line[] = [{ offset: 0, radius: 0.06, stroke: 'dashed' }];

const UP = new Vector3(0, 1, 0);
const RIGHT = new Vector3(1, 0, 0);

export function linesOf(bond: PlacedBond): readonly Line[] {
    return bond.partial ? PARTIAL_LINES : LINES[bond.kind];
}

export function drawsSideLines(kind: BondKind): boolean {
    return LINES[kind].some((line) => line.offset !== 0);
}

export function bondAxis(bond: PlacedBond): Vector3 {
    return bond.to.position.clone().sub(bond.from.position).normalize();
}

export function bondPerpendicular(bond: PlacedBond): Vector3 {
    const axis = bondAxis(bond);
    const center = bond.from.position.clone().add(bond.to.position).multiplyScalar(0.5);
    const perpendicular = bond.sidePoint.clone().sub(center);

    perpendicular.addScaledVector(axis, -perpendicular.dot(axis));

    if (perpendicular.lengthSq() < 1e-6) {
        perpendicular.crossVectors(axis, Math.abs(axis.y) < 0.9 ? UP : RIGHT);
    }

    return perpendicular.normalize();
}