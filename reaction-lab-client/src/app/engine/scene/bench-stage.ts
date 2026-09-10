import { Box3, Fog } from "three";
import { Disposable } from "../core/disposal-scope";
import { Ground } from "../objects/ground";
import { Environment } from "../rendering/environment";
import { LightingRig, TokenResolver } from "../rendering/lighting-rig";
import { EngineContext } from "../core/engine-context";
import { Look } from "../rendering/look";
import { DEFAULT_DISTANCE } from "../core/camera-framing";

export class BenchStage implements Disposable {
    readonly ground = new Ground();
    readonly lights = new LightingRig();

    private readonly environment: Environment;
    private readonly fog = new Fog(0x000000, 20, 46);
    
    private look: Look | null = null;
    private fogScale = 1;

    constructor(private readonly context: EngineContext) {
        this.environment = new Environment(context);
        context.scene.fog = this.fog;
        context.scene.add(this.ground, this.lights);
    }

    applyLook(look: Look, resolve: TokenResolver): void {
        this.context.renderer.toneMappingExposure = look.exposure;
        this.environment.setIntensity(look.environmentIntensity);

        this.fog.color.copy(resolve(look.fog.token));
        this.look = look;
        this.fog.near = look.fog.near * this.fogScale;
        this.fog.far = look.fog.far * this.fogScale;

        this.ground.setColor(resolve(look.ground));
        this.lights.apply(look, resolve);
    }

    fit(distance: number, bounds: Box3): void {
        this.fogScale = distance / DEFAULT_DISTANCE;
        this.ground.scale.setScalar(this.fogScale);

        if (this.look) {
            this.fog.near = this.look.fog.near * this.fogScale;
            this.fog.far = this.look.fog.far * this.fogScale;
        }

        this.lights.fitShadow(bounds);
    }

    dispose(): void {
        this.context.scene.remove(this.ground, this.lights);
        this.context.scene.fog = null;
        this.lights.dispose();
        this.ground.dispose();
        this.environment.dispose();
    }
}