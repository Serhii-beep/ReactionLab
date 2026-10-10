import { Vector3 } from "three";
import { BondKind, UnitBond } from "../../../../engine/scene/bench-layout";

export interface GeometryAtom {
    readonly symbol: string;
    readonly position: Vector3;
}

export interface IonGeometry {
    readonly atoms: readonly GeometryAtom[];
    readonly bonds: readonly UnitBond[];
}

const TETRAHEDRAL_DIRECTIONS: readonly Vector3[] = [
    new Vector3(1, 1, 1),
    new Vector3(-1, -1, 1),
    new Vector3(-1, 1, -1),
    new Vector3(1, -1, -1)
].map((direction) => direction.normalize());
const TRIGONAL_DEGREES = [90, 210, 330];
const CARBOXYLATE_HALF_ANGLE_DEGREES = 62.5;
const HYDROXYL_TURN_DEGREES = 70;
const METHYL_POLAR_COSINE = -1 / 3;
const METHYL_AZIMUTH_DEGREES = [0, 120, 240];

export function monatomic(symbol: string): IonGeometry {
    return { atoms: [{ symbol, position: new Vector3() }], bonds: [] };
}

export function tetrahedral(center: string, ligand: string, lengthAngstrom: number, kind: BondKind): IonGeometry {
    return {
        atoms: [{ symbol: center, position: new Vector3() }, ...TETRAHEDRAL_DIRECTIONS.map((direction) => ({ symbol: ligand, position: direction.clone().multiplyScalar(lengthAngstrom) }))],
        bonds: TETRAHEDRAL_DIRECTIONS.map((_, index) => ({ from: 0, to: index + 1, kind }))
    };
}

export function trigonal(center: string, ligand: string, lengthAngstrom: number): IonGeometry {
    return {
        atoms: [{ symbol: center, position: new Vector3() }, ...TRIGONAL_DEGREES.map((degrees) => ({ symbol: ligand, position: inPlane(degrees, lengthAngstrom) }))],
        bonds: TRIGONAL_DEGREES.map((_, index) => ({ from: 0, to: index + 1, kind: 'aromatic' as const }))
    };
}

export function diatomic(first: string, second: string, lengthAngstrom: number): IonGeometry {
    return {
        atoms: [{ symbol: first, position: new Vector3() }, { symbol: second, position: new Vector3(lengthAngstrom, 0, 0) }],
        bonds: [{ from: 0, to: 1, kind: 'single' }]
    };
}

export function hydroxide(): IonGeometry {
    return {
        atoms: [{ symbol: 'O', position: new Vector3() }, { symbol: 'H', position: new Vector3(0, 0.97, 0) }],
        bonds: [{ from: 0, to: 1, kind: 'single' }]
    };
}

export function bicarbonate(): IonGeometry {
    const hydroxylOxygen = inPlane(TRIGONAL_DEGREES[2], 1.34);

    return {
        atoms: [
            { symbol: 'C', position: new Vector3() },
            { symbol: 'O', position: inPlane(TRIGONAL_DEGREES[0], 1.26) },
            { symbol: 'O', position: inPlane(TRIGONAL_DEGREES[1], 1.26) },
            { symbol: 'O', position: hydroxylOxygen },
            { symbol: 'H', position: inPlane(TRIGONAL_DEGREES[2] + HYDROXYL_TURN_DEGREES, 0.97).add(hydroxylOxygen) }
        ],
        bonds: [
            { from: 0, to: 1, kind: 'aromatic' },
            { from: 0, to: 2, kind: 'aromatic' },
            { from: 0, to: 3, kind: 'single' },
            { from: 3, to: 4, kind: 'single' }
        ]
    };
}

export function acetate(): IonGeometry {
    const methyl = new Vector3(0, -1.52, 0);
    const horizontal = Math.sqrt(1 - METHYL_POLAR_COSINE ** 2);
    const hydrogens = METHYL_AZIMUTH_DEGREES.map((degrees) => {
        const radians = (degrees * Math.PI) / 180;

        return new Vector3(horizontal * Math.cos(radians), METHYL_POLAR_COSINE, horizontal * Math.sin(radians)).multiplyScalar(1.09).add(methyl);
    });

    return {
        atoms: [
            { symbol: 'C', position: new Vector3() },
            { symbol: 'O', position: inPlane(90 - CARBOXYLATE_HALF_ANGLE_DEGREES, 1.26) },
            { symbol: 'O', position: inPlane(90 + CARBOXYLATE_HALF_ANGLE_DEGREES, 1.26) },
            { symbol: 'C', position: methyl },
            ...hydrogens.map((position) => ({ symbol: 'H', position }))
        ],
        bonds: [
            { from: 0, to: 1, kind: 'aromatic' },
            { from: 0, to: 2, kind: 'aromatic' },
            { from: 0, to: 3, kind: 'single' },
            ...hydrogens.map((_, index) => ({ from: 3, to: index + 4, kind: 'single' as const }))
        ]
    };
}

function inPlane(degrees: number, lengthAngstrom: number): Vector3 {
    const radians = (degrees * Math.PI) / 180;

    return new Vector3(Math.cos(radians) * lengthAngstrom, Math.sin(radians) * lengthAngstrom, 0);
}
