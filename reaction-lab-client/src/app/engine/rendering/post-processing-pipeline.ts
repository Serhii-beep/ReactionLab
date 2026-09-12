import { BloomEffect, EffectComposer, EffectPass, RenderPass, SMAAEffect, SMAAPreset, ToneMappingEffect, ToneMappingMode, VignetteEffect } from "postprocessing";
import { Disposable } from "../core/disposal-scope";
import { EngineContext, Presenter } from "../core/engine-context";
import { N8AOPostPass } from "n8ao";
import { QualityTier } from "./quality";
import { ACESFilmicToneMapping, Color, HalfFloatType, NoToneMapping } from "three";

const AO_RADIUS = 0.9;
const AO_FALLOFF = 0.6;
const AO_INTENSITY = 2.2;
const BLOOM_THRESHOLD = 1.0;
const BLOOM_INTENSITY = 0.5;
const VIGNETTE_OFFSET = 0.35;
const MULTISAMPLES = 4;

export class PostProcessingPipeline implements Disposable, Presenter {
    private readonly composer: EffectComposer;
    private readonly occlusion: N8AOPostPass;
    private readonly bloom = new BloomEffect({ luminanceThreshold: BLOOM_THRESHOLD, intensity: BLOOM_INTENSITY, mipmapBlur: true });
    private readonly smaa: EffectPass;
    private readonly vignette = new VignetteEffect({ offset: VIGNETTE_OFFSET, darkness: 0 });

    private tier: QualityTier = 'low';

    constructor(private readonly context: EngineContext) {
        const { renderer, scene, camera } = context;

        this.composer = new EffectComposer(renderer, { frameBufferType: HalfFloatType });
        this.occlusion = new N8AOPostPass(scene, camera, 1, 1);
        this.occlusion.configuration.aoRadius = AO_RADIUS;
        this.occlusion.configuration.distanceFalloff = AO_FALLOFF;
        this.occlusion.configuration.intensity = AO_INTENSITY;
        this.occlusion.configuration.gammaCorrection = false;
        this.smaa = new EffectPass(camera, new SMAAEffect({ preset: SMAAPreset.HIGH }));

        this.composer.addPass(new RenderPass(scene, camera));
        this.composer.addPass(this.occlusion);
        this.composer.addPass(new EffectPass(camera, this.bloom));
        this.composer.addPass(this.smaa);
        this.composer.addPass(new EffectPass(
            camera,
            this.vignette,
            new ToneMappingEffect({ mode: ToneMappingMode.ACES_FILMIC })
        ));
    }

    get quality(): QualityTier {
        return this.tier;
    }

    setQuality(tier: QualityTier): void {
        if (tier === this.tier) {
            return;
        }

        this.tier = tier;

        if (tier === 'low') {
            this.context.renderer.toneMapping = ACESFilmicToneMapping;
            this.context.setPresenter(null);

            return;
        }

        const high = tier === 'high';

        this.composer.multisampling = high ? MULTISAMPLES : 0;
        this.smaa.enabled = !high;
        this.occlusion.configuration.halfRes = !high;
        this.occlusion.setQualityMode(high ? 'High' : 'Medium');
        this.bloom.blendMode.setOpacity(high ? 1 : 0);
        this.context.renderer.toneMapping = NoToneMapping;
        this.context.setPresenter(this);
    }

    setVignette(darkness: number): void {
        this.vignette.darkness = darkness;
    }

    setOcclusionColor(color: Color): void {
        this.occlusion.configuration.color = color;
    }

    render(): void {
        this.composer.render();
    }

    setSize(width: number, height: number): void {
        this.composer.setSize(width, height, false);
    }

    dispose(): void {
        if (this.tier !== 'low') {
            this.setQuality('low');
        }

        this.composer.dispose();
    }
}