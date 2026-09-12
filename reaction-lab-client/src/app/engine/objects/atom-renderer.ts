import { Group, Matrix4 } from "three";
import { Disposable } from "../core/disposal-scope";
import { GeometryCache, Lod } from "../resources/geometry-cache";
import { MaterialCache } from "../resources/material-cache";
import { PlacedAtom } from "../scene/bench-layout";
import { BatchRequest, commit, InstancedBatches } from "./instanced-batches";

export class AtomRenderer implements Disposable {
    private readonly batches = new InstancedBatches('atoms');
    private readonly matrix = new Matrix4();

    constructor(
        private readonly geometries: GeometryCache,
        private readonly materials: MaterialCache
    ) {}

    get root(): Group {
        return this.batches.root;
    }

    render(atoms: readonly PlacedAtom[], lod: Lod): void {
        const groups = new Map<string, PlacedAtom[]>();

        for (const atom of atoms) {
            const key = `${atom.symbol}|${atom.phase}`;
            const group = groups.get(key);

            if (group) {
                group.push(atom);
            } else {
                groups.set(key, [atom]);
            }
        }

        const requests = new Map<string, BatchRequest>();

        for (const [key, group] of groups) {
            const material = this.materials.atom(group[0].symbol, group[0].phase, group[0].color);

            requests.set(key, { material, count: group.length, castShadow: true });
        }

        const meshes = this.batches.sync(this.geometries.sphere(lod), requests);

        for (const [key, group] of groups) {
            const mesh = meshes.get(key);

            if (!mesh) {
                continue;
            }

            group.forEach((atom, index) => {
                this.matrix.makeScale(atom.radius, atom.radius, atom.radius).setPosition(atom.position);
                mesh.setMatrixAt(index, this.matrix);
            });
            commit(mesh);
        }
    }

    dispose(): void {
        this.batches.dispose();
    }
}