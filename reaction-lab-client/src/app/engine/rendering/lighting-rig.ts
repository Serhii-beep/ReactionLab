import { Color, DirectionalLight, Group, HemisphereLight } from "three";
import { Look } from "./look";

export type TokenResolver = (token: string) => Color;

const SHADOW_EXTENT = 14;

export class LightingRig extends Group {
    private readonly key = new DirectionalLight();
    private readonly fill = new HemisphereLight();
    private readonly rim = new DirectionalLight();

    constructor() {
        super();

        this.name = 'lighting';

        this.key.position.set(8, 14, 10);
        this.key.castShadow = true;
        this.key.shadow.mapSize.set(2048, 2048);
        this.key.shadow.camera.near = 1;
        this.key.shadow.camera.far = 60;
        this.key.shadow.camera.left = -SHADOW_EXTENT;
        this.key.shadow.camera.right = SHADOW_EXTENT;
        this.key.shadow.camera.top = SHADOW_EXTENT;
        this.key.shadow.camera.bottom = -SHADOW_EXTENT;
        this.key.shadow.camera.updateProjectionMatrix();
        this.key.shadow.bias = -0.0004;
        this.key.shadow.normalBias = 0.02;
        this.key.shadow.radius = 3;

        this.rim.position.set(-10, 8, -12);

        this.add(this.key, this.key.target, this.fill, this.rim);
    }

    apply(look: Look, resolve: TokenResolver): void {
        this.key.intensity = look.key.intensity;
        this.key.color.set(look.key.color);
        this.key.shadow.intensity = look.shadowIntensity;

        this.fill.intensity = look.fill.intensity;
        this.fill.color.copy(resolve(look.fill.sky));
        this.fill.groundColor.copy(resolve(look.fill.ground));

        this.rim.intensity = look.rim.intensity;
        this.rim.color.set(look.rim.color);
    }

    dispose(): void {
        this.key.shadow.dispose();
    }
}