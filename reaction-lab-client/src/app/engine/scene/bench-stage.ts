import { Box3, Fog } from "three";
import { Disposable } from "../core/disposal-scope";
import { Ground } from "../objects/ground";
import { Environment } from "../rendering/environment";
import { LightingRig, TokenResolver } from "../rendering/lighting-rig";
import { EngineContext } from "../core/engine-context";
import { LIT_WHITE_LUMINANCE, Look } from "../rendering/look";
import { referenceDistance } from "../core/camera-framing";
import { PostProcessingPipeline } from "../rendering/post-processing-pipeline";
import { PlacedAtom } from "./bench-layout";
import { incandescentLuminanceOf } from "../rendering/incandescence";

const OCCLUSION_DARKENING = 0.3;

export class BenchStage implements Disposable {
    readonly ground = new Ground();
    readonly lights = new LightingRig();

    private readonly environment: Environment;
    private readonly fog = new Fog(0x000000, 20, 46);
    
    private look: Look | null = null;
    private fogScale = 1;
    private glowExposureScale = 1;

    constructor(
        private readonly context: EngineContext,
        private readonly pipeline: PostProcessingPipeline
    ) {
        this.environment = new Environment(context);
        context.scene.fog = this.fog;
        context.scene.add(this.ground, this.lights);
    }

    applyLook(look: Look, resolve: TokenResolver): void {
        this.look = look;
        this.expose();
        this.environment.setIntensity(look.environmentIntensity);

        this.fog.color.copy(resolve(look.fog.token));
        this.fog.near = look.fog.near * this.fogScale;
        this.fog.far = look.fog.far * this.fogScale;

        this.ground.setColor(resolve(look.ground));
        this.lights.apply(look, resolve);

        this.pipeline.setVignette(look.vignette);
        this.pipeline.setOcclusionColor(resolve(look.fog.token).multiplyScalar(OCCLUSION_DARKENING));
    }

    exposeFor(atoms: readonly PlacedAtom[]): void {
        const brightest = atoms.reduce((most, atom) => Math.max(most, incandescentLuminanceOf(atom.temperatureKelvin)), 0);

        this.glowExposureScale = brightest > LIT_WHITE_LUMINANCE ? LIT_WHITE_LUMINANCE / brightest : 1;
        this.expose();
    }

    fit(distance: number, bounds: Box3): void {
        this.fogScale = distance / referenceDistance(this.context.camera);
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

    private expose(): void {
        if (this.look !== null) {
            this.context.renderer.toneMappingExposure = this.look.exposure * this.glowExposureScale;
        }
    }
}