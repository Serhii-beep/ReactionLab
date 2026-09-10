import { BufferGeometry, CylinderGeometry, SphereGeometry } from "three";
import { Disposable } from "../core/disposal-scope";

export type Lod = 'high' | 'medium' | 'low';

const SPHERE_SEGMENTS: Readonly<Record<Lod, readonly [number, number]>> = {
    high: [48, 32],
    medium: [24, 16],
    low: [12, 8]
};

const CYLINDER_SEGMENTS: Readonly<Record<Lod, number>> = {
    high: 32,
    medium: 16,
    low: 8
};

const HIGH_LOD_PIXELS = 20;
const MEDIUM_LOD_PIXELS = 8;

export function lodFor(pixels: number): Lod {
    if (pixels >= HIGH_LOD_PIXELS) {
        return 'high';
    }

    return pixels >= MEDIUM_LOD_PIXELS ? 'medium' : 'low';
}

export class GeometryCache implements Disposable {
    private readonly geometries = new Map<string, BufferGeometry>();

    get size(): number {
        return this.geometries.size;
    }

    sphere(lod: Lod): BufferGeometry {
        return this.get(`sphere:${lod}`, () => new SphereGeometry(1, ...SPHERE_SEGMENTS[lod]));
    }

    cylinder(lod: Lod): BufferGeometry {
        return this.get(`cylinder:${lod}`, () => new CylinderGeometry(1, 1, 1, CYLINDER_SEGMENTS[lod], 1, true));
    }

    dash(lod: Lod): BufferGeometry {
        return this.get(`dash:${lod}`, () => new CylinderGeometry(1, 1, 1, CYLINDER_SEGMENTS[lod], 1, false));
    }

    dispose(): void {
        for (const geometry of this.geometries.values()) {
            geometry.dispose();
        }

        this.geometries.clear();
    }

    private get(key: string, create: () => BufferGeometry): BufferGeometry {
        let geometry = this.geometries.get(key);

        if (!geometry) {
            geometry = create();
            this.geometries.set(key, geometry);
        }

        return geometry;
    }
}