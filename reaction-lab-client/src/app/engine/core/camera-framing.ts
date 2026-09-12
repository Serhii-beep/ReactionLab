import { Box3, MathUtils, PerspectiveCamera, Sphere, Vector3 } from "three";

export interface Framing {
    readonly center: Vector3;
    readonly distance: number;
}

const MARGIN = 1.25;
const MIN_RADIUS = 4.3;
const EMPTY = new Box3();

export function distanceFor(camera: PerspectiveCamera, radius: number, margin = MARGIN): number {
    const vertical = MathUtils.degToRad(camera.fov) / 2;
    const horizontal = Math.atan(Math.tan(vertical) * camera.aspect);

    return (radius * margin) / Math.sin(Math.min(vertical, horizontal));
}

export function framingFor(camera: PerspectiveCamera, bounds: Box3): Framing {
    const sphere = new Sphere();

    if (!bounds.isEmpty()) {
        bounds.getBoundingSphere(sphere);
    }

    return { center: sphere.center, distance: distanceFor(camera, Math.max(sphere.radius, MIN_RADIUS)) };
}

export function referenceDistance(camera: PerspectiveCamera): number {
    return framingFor(camera, EMPTY).distance;
}