import { Box3, MathUtils, PerspectiveCamera, Sphere, Vector3 } from "three";

export interface Framing {
    readonly center: Vector3;
    readonly distance: number;
}

const MARGIN = 1.25;
const MIN_RADIUS = 4.3;

export function distanceFor(camera: PerspectiveCamera, radius: number, margin = MARGIN): number {
    return radius * margin * distancePerRadius(camera);
}

export function distancePerRadius(camera: PerspectiveCamera): number {
    return distancePerRadiusThrough(camera.getEffectiveFOV(), camera.aspect);
}

export function framingFor(camera: PerspectiveCamera, bounds: Box3): Framing {
    const framed = framedSphereOf(bounds);

    return { center: framed.center, distance: framed.radius * distancePerRadius(camera) };
}

export function framedSphereOf(bounds: Box3): Sphere {
    const sphere = new Sphere();

    if (!bounds.isEmpty()) {
        bounds.getBoundingSphere(sphere);
    }

    sphere.radius = Math.max(sphere.radius, MIN_RADIUS) * MARGIN;

    return sphere;
}

export function referenceDistance(camera: PerspectiveCamera): number {
    return MIN_RADIUS * MARGIN * distancePerRadiusThrough(camera.fov, camera.aspect);
}

function distancePerRadiusThrough(fovDegrees: number, aspect: number): number {
    const vertical = MathUtils.degToRad(fovDegrees) / 2;
    const horizontal = Math.atan(Math.tan(vertical) * aspect);

    return 1 / Math.sin(Math.min(vertical, horizontal));
}