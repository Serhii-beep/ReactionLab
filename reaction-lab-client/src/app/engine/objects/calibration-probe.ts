import { Group, Mesh, MeshStandardMaterial, SphereGeometry } from "three";

const CPK = [0xffffff, 0x909090, 0x3050f8, 0xff0d0d, 0xffff30, 0x1ff01f, 0xab5cf2, 0xe06633];
const RADIUS = 0.8;
const SPACING = 2.2;

export class CalibrationProbe extends Group {
    private readonly geometry = new SphereGeometry(RADIUS, 48, 32);
    private readonly materials = CPK.map((color) => new MeshStandardMaterial({ color, roughness: 0.4, metalness: 0 }));

    constructor() {
        super();

        this.name = 'calibration-probe';

        this.materials.forEach((material, index) => {
            const sphere = new Mesh(this.geometry, material);

            sphere.position.set((index - (CPK.length - 1) / 2) * SPACING, RADIUS, 0);
            sphere.castShadow = true;
            this.add(sphere);
        });
    }

    dispose(): void {
        this.removeFromParent();
        this.geometry.dispose();

        for (const material of this.materials) {
            material.dispose();
        }
    }
}