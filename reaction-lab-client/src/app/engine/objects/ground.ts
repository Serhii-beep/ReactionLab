import { CircleGeometry, Color, Mesh, MeshStandardMaterial } from "three";

export class Ground extends Mesh<CircleGeometry, MeshStandardMaterial> {
    constructor() {
        super(new CircleGeometry(40, 128), new MeshStandardMaterial({ roughness: 0.95, metalness: 0 }));

        this.name = 'ground';
        this.rotation.x = -Math.PI / 2;
        this.receiveShadow = true;
    }

    setColor(color: Color): void {
        this.material.color.copy(color);
    }

    dispose(): void {
        this.geometry.dispose();
        this.material.dispose();
    }
}