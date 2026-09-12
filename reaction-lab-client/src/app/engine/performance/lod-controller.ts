import { projectedRadius } from "../core/projection";
import { Lod, lodFor } from "../resources/geometry-cache";

const LOD_ORDER: readonly Lod[] = ['low', 'medium', 'high'];

export class LodController {
    private ceiling: Lod = 'high';

    get lodCeiling(): Lod {
        return this.ceiling;
    }

    setCeiling(ceiling: Lod): void {
        this.ceiling = ceiling;
    }

    choose(smallestAtomRadius: number, cameraDistance: number, fovDegrees: number, viewportHeight: number): Lod {
        const wanted = lodFor(projectedRadius(smallestAtomRadius, cameraDistance, fovDegrees, viewportHeight));

        return LOD_ORDER.indexOf(wanted) > LOD_ORDER.indexOf(this.ceiling) ? this.ceiling : wanted;
    }
}