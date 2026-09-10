import { Box3, MathUtils, PerspectiveCamera, Sphere, Vector3 } from "three";

export interface Framing {
    readonly center: Vector3;
    readonly distance: number;
}

export const DEFAULT_DISTANCE = 15.65;

const MARGIN = 1.25;
const MIN_RADIUS = 4.3;

const sphere = new Sphere();

export function framingFor(camera: PerspectiveCamera, bounds: Box3): Framing {
    if (bounds.isEmpty()) {
        sphere.center.set(0, 0, 0);
        sphere.radius = 0;
    } else {
        bounds.getBoundingSphere(sphere);
    }

    const vertical = MathUtils.degToRad(camera.fov) / 2;
    const horizontal = Math.atan(Math.tan(vertical) * camera.aspect);
    const radius = Math.max(sphere.radius, MIN_RADIUS);

    return {
        center: sphere.center.clone(),
        distance: (radius * MARGIN) / Math.sin(Math.min(vertical, horizontal))
    };
}