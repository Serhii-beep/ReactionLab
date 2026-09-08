import { Group, InstancedMesh, Matrix4 } from "three";
import { Disposable } from "../core/disposal-scope";
import { GeometryCache, Lod } from "../resources/geometry-cache";
import { MaterialCache } from "../resources/material-cache";
import { PlacedAtom } from "../scene/bench-layout";

const MIN_CAPACITY = 16;

export class AtomRenderer implements Disposable {
    readonly root = new Group();

    private readonly batches = new Map<string, InstancedMesh>();
    private readonly matrix = new Matrix4();

    constructor(
        private readonly geometries: GeometryCache,
        private readonly materials: MaterialCache
    ) {
        this.root.name = 'atoms';
    }

    render(atoms: readonly PlacedAtom[], lod: Lod): void {
        const groups = new Map<string, PlacedAtom[]>();

        for (const atom of atoms) {
            const key = `${atom.symbol}|${atom.phase}`;
            let group = groups.get(key);

            if (!group) {
                group = [];
                groups.set(key, group);
            }

            group.push(atom);
        }

        for (const [key, mesh] of this.batches) {
            if (!groups.has(key)) {
                this.drop(key, mesh);
            }
        }

        for (const [key, group] of groups) {
            this.fill(this.batch(key, group, lod), group);
        }
    }

    dispose(): void {
        for (const [key, mesh] of this.batches) {
            this.drop(key, mesh);
        }

        this.root.removeFromParent();
    }

    private batch(key: string, group: readonly PlacedAtom[], lod: Lod): InstancedMesh {
        const geometry = this.geometries.sphere(lod);
        const existing = this.batches.get(key);

        if (existing && existing.geometry === geometry && existing.instanceMatrix.count >= group.length) {
            return existing;
        }

        if (existing) {
            this.drop(key, existing);
        }

        const [sample] = group;
        const capacity = Math.max(MIN_CAPACITY, 2 ** Math.ceil(Math.log2(group.length)));
        const mesh = new InstancedMesh(geometry, this.materials.atom(sample.symbol, sample.phase, sample.color), capacity);

        mesh.name = key;
        mesh.castShadow = true;
        mesh.receiveShadow = true;
        this.batches.set(key, mesh);
        this.root.add(mesh);

        return mesh;
    }

    private fill(mesh: InstancedMesh, group: readonly PlacedAtom[]): void {
        group.forEach((atom, index) => {
            this.matrix.makeScale(atom.radius, atom.radius, atom.radius).setPosition(atom.position);
            mesh.setMatrixAt(index, this.matrix);
        });

        mesh.count = group.length;
        mesh.instanceMatrix.needsUpdate = true;
        mesh.computeBoundingSphere();
    }

    private drop(key: string, mesh: InstancedMesh): void {
        this.root.remove(mesh);
        mesh.dispose();
        this.batches.delete(key);
    }
}