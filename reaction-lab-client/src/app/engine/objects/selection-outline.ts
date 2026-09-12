import { BackSide, Color, Group, InstancedMesh, Matrix4, MeshBasicMaterial } from "three";
import { Disposable } from "../core/disposal-scope";
import { CylinderTransform } from "./cylinder-transform";
import { PlacedAtom, PlacedBond } from "../scene/bench-layout";
import { GeometryCache } from "../resources/geometry-cache";
import { bondExtent } from "./bond-renderer";

export type HighlightLevelsByUnitId = ReadonlyMap<string, number>;

const CAPACITY = 256;

export class SelectionOutline implements Disposable {
    readonly root = new Group();

    private readonly material = new MeshBasicMaterial({ side: BackSide, toneMapped: false });
    private readonly spheres: InstancedMesh;
    private readonly sticks: InstancedMesh;
    private readonly matrix = new Matrix4();
    private readonly cylinder = new CylinderTransform();

    private atoms: readonly PlacedAtom[] = [];
    private bonds: readonly PlacedBond[] = [];

    constructor(geometries: GeometryCache) {
        this.spheres = new InstancedMesh(geometries.sphere('medium'), this.material, CAPACITY);
        this.sticks = new InstancedMesh(geometries.cylinder('low'), this.material, CAPACITY);
        this.spheres.count = 0;
        this.sticks.count = 0;
        this.spheres.frustumCulled = false;
        this.sticks.frustumCulled = false;
        this.root.name = 'selection-outline';
        this.root.add(this.spheres, this.sticks);
    }

    setAccent(color: Color): void {
        this.material.color.copy(color);
    }

    render(atoms: readonly PlacedAtom[], bonds: readonly PlacedBond[]): void {
        this.atoms = atoms;
        this.bonds = bonds;
    }

    update(highlightLevels: HighlightLevelsByUnitId, thickness: number): void {
        let spheres = 0;
        let sticks = 0;

        for (const atom of this.atoms) {
            const level = highlightLevels.get(atom.unitId);

            if (!level || spheres === CAPACITY) {
                continue;
            }

            const radius = atom.radius + thickness * level;

            this.matrix.makeScale(radius, radius, radius).setPosition(atom.position);
            this.spheres.setMatrixAt(spheres, this.matrix);
            spheres++;
        }

        for (const bond of this.bonds) {
            const highlightLevel = highlightLevels.get(bond.from.unitId);

            if (!highlightLevel || sticks === CAPACITY) {
                continue;
            }

            const radius = bondExtent(bond.kind) + thickness * highlightLevel;

            this.sticks.setMatrixAt(sticks, this.cylinder.between(bond.from.position, bond.to.position, radius));
            sticks++;
        }

        this.spheres.count = spheres;
        this.sticks.count = sticks;
        this.spheres.instanceMatrix.needsUpdate = true;
        this.sticks.instanceMatrix.needsUpdate = true;
    }

    dispose(): void {
        this.spheres.dispose();
        this.sticks.dispose();
        this.material.dispose();
        this.root.removeFromParent();
    }
}