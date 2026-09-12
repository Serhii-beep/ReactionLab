import { Matrix4, Quaternion, Vector3 } from "three";

const UP = new Vector3(0, 1, 0);

export class CylinderTransform {
    private readonly axis = new Vector3();
    private readonly center = new Vector3();
    private readonly quaternion = new Quaternion();
    private readonly scale = new Vector3();
    private readonly matrix = new Matrix4();

    between(start: Vector3, end: Vector3, radius: number): Matrix4 {
        this.axis.copy(end).sub(start);

        const length = this.axis.length();

        if (length === 0) {
            return this.matrix.compose(start, this.quaternion.identity(), this.scale.setScalar(radius));
        }

        this.quaternion.setFromUnitVectors(UP, this.axis.divideScalar(length));
        this.center.copy(start).add(end).multiplyScalar(0.5);
        this.scale.set(radius, length, radius);

        return this.matrix.compose(this.center, this.quaternion, this.scale);
    }
}