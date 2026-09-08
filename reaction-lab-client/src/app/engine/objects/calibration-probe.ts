import { Color, Group, Mesh } from "three";
import { Disposable } from "../core/disposal-scope";
import { GeometryCache } from "../resources/geometry-cache";
import { MaterialCache } from "../resources/material-cache";
import { LabelAtlas } from "../resources/label-atlas";

export interface ProbeSample {
    symbol: string;
    color: Color;
}

const RADIUS = 0.8;
const SPACING = 2.2;

export class CalibrationProbe extends Group implements Disposable {
    private readonly releases: (() => void)[] = [];

    constructor(
        samples: readonly ProbeSample[],
        geometries: GeometryCache,
        materials: MaterialCache,
        labels: LabelAtlas,
        labelColor: Color
    ) {
        super();

        this.name = 'calibration-probe';
        
        samples.forEach((sample, index) => {
            const x = (index - (samples.length - 1) / 2) * SPACING;
            const sphere = new Mesh(geometries.sphere('high'), materials.atom(sample.symbol, 'solid', sample.color));
            const label = labels.create(sample.symbol, { size: 0.55, color: labelColor });

            sphere.position.set(x, RADIUS, 0);
            sphere.scale.setScalar(RADIUS);
            sphere.castShadow = true;
            label.position.set(x, RADIUS * 2 + 0.6, 0);

            this.releases.push(() => labels.release(label));
            this.add(sphere, label);
        })
    }

    dispose(): void {
        this.removeFromParent();
        
        for (const release of this.releases) {
            release();
        }
    }
}