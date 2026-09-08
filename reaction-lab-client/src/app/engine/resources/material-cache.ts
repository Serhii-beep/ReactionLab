import { Color, MeshPhysicalMaterial } from "three";
import { Disposable } from "../core/disposal-scope";

export type Phase = 'solid' | 'liquid' | 'gas' | 'aqueous' | 'plasma';

interface Finish {
    roughness: number;
    clearcoat: number;
    opacity: number;
    emissive: number;
}

const FINISHES: Readonly<Record<Phase, Finish>> = {
    solid: { roughness: 0.35, clearcoat: 0.15, opacity: 1, emissive: 0 },
    liquid: { roughness: 0.2, clearcoat: 0.4, opacity: 1, emissive: 0 },
    gas: { roughness: 0.3, clearcoat: 0, opacity: 0.55, emissive: 0 },
    aqueous: { roughness: 0.25, clearcoat: 0.3, opacity: 0.8, emissive: 0 },
    plasma: { roughness: 0.5, clearcoat: 0, opacity: 1, emissive: 0.6 },
};

export class MaterialCache implements Disposable {
    private readonly materials = new Map<string, MeshPhysicalMaterial>();

    get size(): number {
        return this.materials.size;
    }

    atom(symbol: string, phase: Phase, color: Color): MeshPhysicalMaterial {
        return this.get(`${symbol}|${phase}`, () => this.create(color, FINISHES[phase]));
    }

    bond(color: Color): MeshPhysicalMaterial {
        return this.get('bond', () => this.create(color, FINISHES.solid));
    }

    dispose(): void {
        for (const material of this.materials.values()) {
            material.dispose();
        }

        this.materials.clear();
    }

    private create(color: Color, finish: Finish): MeshPhysicalMaterial {
        const material = new MeshPhysicalMaterial({
            color,
            metalness: 0,
            roughness: finish.roughness,
            clearcoat: finish.clearcoat,
            transparent: finish.opacity < 1,
            opacity: finish.opacity
        });

        material.emissive.copy(color).multiplyScalar(finish.emissive);

        return material;
    }

    private get(key: string, create: () => MeshPhysicalMaterial): MeshPhysicalMaterial {
        let material = this.materials.get(key);

        if (!material) {
            material = create();
            this.materials.set(key, material);
        }

        return material;
    }
}