import { BackSide, Color, Group, InstancedMesh, Matrix4, MeshBasicMaterial, Vector3 } from "three";
import { Disposable } from "../core/disposal-scope";
import { CylinderTransform } from "./cylinder-transform";
import { PlacedAtom, PlacedBond } from "../scene/bench-layout";
import { GeometryCache } from "../resources/geometry-cache";
import { BondPiece, bondPieces } from "./bond-renderer";

export type HighlightLevelsByUnitId = ReadonlyMap<string, number>;

interface OutlinedBond {
    readonly unitId: string;
    readonly pieces: readonly BondPiece[];
}

const SPHERE_CAPACITY = 320;
const CYLINDER_CAPACITY = 512;

export class SelectionOutline implements Disposable {
    readonly root = new Group();

    private readonly material = new MeshBasicMaterial({ side: BackSide, toneMapped: false });
    private readonly sphereHulls: InstancedMesh;
    private readonly cylinderHulls: InstancedMesh;
    private readonly matrix = new Matrix4();
    private readonly cylinder = new CylinderTransform();

    private atoms: readonly PlacedAtom[] = [];
    private outlinedBonds: readonly OutlinedBond[] = [];
    private spheresUsed = 0;
    private cylindersUsed = 0;

    constructor(geometries: GeometryCache) {
        this.sphereHulls = new InstancedMesh(geometries.sphere('medium'), this.material, SPHERE_CAPACITY);
        this.cylinderHulls = new InstancedMesh(geometries.cylinder('low'), this.material, CYLINDER_CAPACITY);
        this.sphereHulls.count = 0;
        this.cylinderHulls.count = 0;
        this.sphereHulls.frustumCulled = false;
        this.cylinderHulls.frustumCulled = false;
        this.root.name = 'selection-outline';
        this.root.add(this.sphereHulls, this.cylinderHulls);
    }

    setAccent(color: Color): void {
        this.material.color.copy(color);
    }

    render(atoms: readonly PlacedAtom[], bonds: readonly PlacedBond[]): void {
        this.atoms = atoms;
        this.outlinedBonds = bonds.map((bond) => ({ unitId: bond.from.unitId, pieces: bondPieces(bond) }));
    }

    update(highlightLevels: HighlightLevelsByUnitId, thickness: number): void {
        this.spheresUsed = 0;
        this.cylindersUsed = 0;

        for (const atom of this.atoms) {
            const highlightLevel = highlightLevels.get(atom.unitId);

            if (highlightLevel) {
                this.sphereHull(atom.position, atom.radius + thickness * highlightLevel)
            }
        }

        for (const bond of this.outlinedBonds) {
            const highlightLevel = highlightLevels.get(bond.unitId);

            if (highlightLevel) {
                this.bondHulls(bond.pieces, thickness * highlightLevel);
            }
        }

        this.sphereHulls.count = this.spheresUsed;
        this.cylinderHulls.count = this.cylindersUsed;
        this.sphereHulls.instanceMatrix.needsUpdate = true;
        this.cylinderHulls.instanceMatrix.needsUpdate = true;
    }

    dispose(): void {
        this.sphereHulls.dispose();
        this.cylinderHulls.dispose();
        this.material.dispose();
        this.root.removeFromParent();
    }

    private bondHulls(pieces: readonly BondPiece[], growth: number): void {
        for (const piece of pieces) {
            if (piece.start.equals(piece.end)) {
                this.sphereHull(piece.start, piece.radius + growth);
            } else {
                this.cylinderHull(piece.start, piece.end, piece.radius + growth);
            }
        }
    }

    private sphereHull(position: Vector3, radius: number): void {
        if (this.spheresUsed === SPHERE_CAPACITY) {
            return;
        }

        this.matrix.makeScale(radius, radius, radius).setPosition(position);
        this.sphereHulls.setMatrixAt(this.spheresUsed, this.matrix);
        this.spheresUsed++;
    }

    private cylinderHull(start: Vector3, end: Vector3, radius: number): void {
        if (this.cylindersUsed === CYLINDER_CAPACITY) {
            return;
        }

        this.cylinderHulls.setMatrixAt(this.cylindersUsed, this.cylinder.between(start, end, radius));
        this.cylindersUsed++;
    }
}