import { PerspectiveCamera, Sphere, Vector3 } from "three";
import { projectedRadius } from "../core/projection";

export interface UnitAnchor {
    readonly centerX: number;
    readonly centerY: number;
    readonly radiusPixels: number;
    readonly viewportWidth: number;
    readonly viewportHeight: number;
    readonly onScreen: boolean;
}

const OFFSET_MARGIN = 1.5;

export function unitAnchorOf(sphere: Sphere, camera: PerspectiveCamera, viewportWidth: number, viewportHeight: number, projectedCenter: Vector3): UnitAnchor {
    const ndc = projectedCenter.copy(sphere.center).project(camera);

    return {
        centerX: ((ndc.x + 1) / 2) * viewportWidth,
        centerY: ((1 - ndc.y) / 2) * viewportHeight,
        radiusPixels: projectedRadius(sphere.radius, camera.position.distanceTo(sphere.center), camera.fov, viewportHeight),
        viewportWidth,
        viewportHeight,
        onScreen: ndc.z < 1 && Math.abs(ndc.x) <= OFFSET_MARGIN && Math.abs(ndc.y) <= OFFSET_MARGIN
    };
}
