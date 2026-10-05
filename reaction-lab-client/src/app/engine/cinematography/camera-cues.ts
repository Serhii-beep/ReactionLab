import { Box3, Sphere, Vector3 } from "three";

export interface CameraCues {
    readonly gathered: Sphere;
    readonly finalBounds: Box3;
    readonly releaseSeconds: number;
    readonly shownEnthalpyKilojoulesPerMole: number;
    reachAt(seconds: number, center: Vector3): number;
}