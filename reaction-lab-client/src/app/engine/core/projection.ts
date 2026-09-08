import { MathUtils } from "three";

export function projectedRadius(radius: number, distance: number, fovDegrees: number, viewportHeight: number): number {
    return (radius / (distance * Math.tan(MathUtils.degToRad(fovDegrees) / 2))) * (viewportHeight / 2);
}