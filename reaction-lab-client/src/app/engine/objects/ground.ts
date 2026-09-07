import { CircleGeometry, Color, Mesh, MeshBasicMaterial } from "three";

export class Ground extends Mesh<CircleGeometry, MeshBasicMaterial> {
    constructor() {
        super(new CircleGeometry(12, 96), new MeshBasicMaterial());

        this.name = 'ground';
        this.rotation.x = -Math.PI / 2;
    }

    setColor(color: Color): void {
        this.material.color.copy(color);
    }
}