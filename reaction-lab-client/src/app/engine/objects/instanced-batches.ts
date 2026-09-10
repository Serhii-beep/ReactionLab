import { BufferGeometry, Group, InstancedMesh, Material } from "three";
import { Disposable } from "../core/disposal-scope";

export interface BatchRequest {
    readonly material: Material;
    readonly count: number;
    readonly castShadow: boolean;
}

const MIN_CAPACITY = 16;

export class InstancedBatches implements Disposable {
    readonly root = new Group();

    private readonly meshes = new Map<string, InstancedMesh>();

    constructor(name: string) {
        this.root.name = name;
    }

    sync(geometry: BufferGeometry, requests: ReadonlyMap<string, BatchRequest>): ReadonlyMap<string, InstancedMesh> {
        for (const [key, mesh] of this.meshes) {
            if (!requests.has(key)) {
                this.drop(key, mesh);
            }
        }

        const result = new Map<string, InstancedMesh>();

        for (const [key, request] of requests) {
            result.set(key, this.batch(key, geometry, request));
        }

        return result;
    }

    dispose(): void {
        for (const [key, mesh] of this.meshes) {
            this.drop(key, mesh);
        }

        this.root.removeFromParent();
    }

    private batch(key: string, geometry: BufferGeometry, request: BatchRequest): InstancedMesh {
        const existing = this.meshes.get(key);

        if (existing && existing.geometry === geometry && existing.material === request.material
            && existing.instanceMatrix.count >= request.count) {
            existing.count = request.count;

            return existing;
        }

        if (existing) {
            this.drop(key, existing);
        }

        const capacity = Math.max(MIN_CAPACITY, 2 ** Math.ceil(Math.log2(Math.max(request.count, 1))));
        const mesh = new InstancedMesh(geometry, request.material, capacity);

        mesh.name = key;
        mesh.count = request.count;
        mesh.castShadow = request.castShadow;
        mesh.receiveShadow = true;
        this.meshes.set(key, mesh);
        this.root.add(mesh);

        return mesh;
    }

    private drop(key: string, mesh: InstancedMesh): void {
        this.root.remove(mesh);
        mesh.dispose();
        this.meshes.delete(key);
    }
}

export function commit(mesh: InstancedMesh): void {
    mesh.instanceMatrix.needsUpdate = true;
    mesh.computeBoundingSphere();
}