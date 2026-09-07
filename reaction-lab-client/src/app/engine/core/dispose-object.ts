import { BufferGeometry, Material, Object3D, Texture } from 'three';

interface Renderable {
    geometry?: BufferGeometry;
    material?: Material | Material[];
}

export function disposeObject(object: Object3D): void {
    const { geometry, material } = object as Object3D & Renderable;

    geometry?.dispose();

    for (const item of Array.isArray(material) ? material : material ? [material] : []) {
        disposeMaterial(item);
    }
}

function disposeMaterial(material: Material): void {
    for (const value of Object.values(material)) {
        if (value instanceof Texture) {
            value.dispose();
        }
    }

    material.dispose();
}