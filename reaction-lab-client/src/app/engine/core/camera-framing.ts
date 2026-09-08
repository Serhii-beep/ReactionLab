import { Box3, MathUtils, PerspectiveCamera, Sphere, Vector3 } from "three";

export const DEFAULT_DISTANCE = 15.65;

const VIEW_DIRECTION = new Vector3(0, 7, 14).normalize();
const ORIGIN = new Vector3();
const MARGIN = 1.25;

const sphere = new Sphere();

export function frameBounds(camera: PerspectiveCamera, bounds: Box3): number {
    if (bounds.isEmpty()) {
        sphere.set(ORIGIN, 0);
    } else {
        bounds.getBoundingSphere(sphere);
    }

    const vertical = MathUtils.degToRad(camera.fov) / 2;
    const horizontal = Math.atan(Math.tan(vertical) * camera.aspect);
    const distance = Math.max(DEFAULT_DISTANCE, (sphere.radius * MARGIN) / Math.sin(Math.min(vertical, horizontal)));

    camera.position.copy(VIEW_DIRECTION).multiplyScalar(distance).add(sphere.center);
    camera.lookAt(sphere.center);

    return distance;
}