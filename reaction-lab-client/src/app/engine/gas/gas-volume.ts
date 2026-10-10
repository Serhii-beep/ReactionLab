import { Color, HalfFloatType, IUniform, Matrix4, PerspectiveCamera, ShaderMaterial, Texture, WebGL3DRenderTarget, WebGLRenderer, WebGLRenderTarget } from "three";
import { Pass } from "postprocessing";
import { LIT_WHITE_LUMINANCE } from "../rendering/look";
import { VOLUME_COMPOSITE_SHADER, VOLUME_MARCH_SHADER, VOLUME_VERTEX_SHADER } from "./gas-volume-shaders";
import { renderDetailNoise } from "./detail-noise";
import { GasSolver } from "./gas-solver";
import { GasLook } from "./gas-look";

interface MarchUniforms {
    readonly uScalars: IUniform<Texture | null>;
    readonly uLighting: IUniform<Texture | null>;
    readonly uCoordinatesA: IUniform<Texture | null>;
    readonly uCoordinatesB: IUniform<Texture | null>;
    readonly uFlowWeight: IUniform<number>;
    readonly uNoise: IUniform<Texture | null>;
    readonly uDepth: IUniform<Texture | null>;
    readonly uProjectionInverse: IUniform<Matrix4>;
    readonly uCameraWorld: IUniform<Matrix4>;
    readonly uCameraNear: IUniform<number>;
    readonly uCameraFar: IUniform<number>;
    readonly uKeyLight: IUniform<Color>;
    readonly uSkyLight: IUniform<Color>;
    readonly uFlameLight: IUniform<Color>;
    readonly uCloudAlbedo: IUniform<Color>;
    readonly uFade: IUniform<number>;
}

const MARCH_RESOLUTION_SCALE = 0.5;
const KEY_LIGHT_SHARE = 0.4;
const SKY_LIGHT_SHARE = 0.55;
const WHITE_ALBEDO = new Color(1, 1, 1);

export class GasVolume extends Pass {
    private readonly marchTarget = new WebGLRenderTarget(1, 1, { type: HalfFloatType, depthBuffer: false });
    private readonly marchUniforms: MarchUniforms = marchUniformsOf();
    private readonly march = new ShaderMaterial({ vertexShader: VOLUME_VERTEX_SHADER, fragmentShader: VOLUME_MARCH_SHADER, uniforms: { ...this.marchUniforms }, depthTest: false, depthWrite: false });
    private readonly composite: ShaderMaterial;
    private noise: WebGL3DRenderTarget | null = null;

    constructor(private readonly viewCamera: PerspectiveCamera) {
        super('GasVolume');

        this.composite = new ShaderMaterial({
            vertexShader: VOLUME_VERTEX_SHADER,
            fragmentShader: VOLUME_COMPOSITE_SHADER,
            uniforms: { inputBuffer: { value: null }, uGas: { value: this.marchTarget.texture } },
            depthTest: false,
            depthWrite: false
        });
        this.fullscreenMaterial = this.composite;
        this.needsDepthTexture = true;
        this.enabled = false;
    }

    show(solver: GasSolver, look: GasLook, cloudAlbedo: Color | null): void {
        Object.assign(this.march.uniforms, solver.grid, solver.box, solver.mist);
        this.marchUniforms.uFlameLight.value.copy(look.flameColor).multiplyScalar(look.flameLuminance * LIT_WHITE_LUMINANCE);
        this.marchUniforms.uCloudAlbedo.value.copy(cloudAlbedo ?? WHITE_ALBEDO);
        this.marchUniforms.uFade.value = 1;
        this.track(solver);
        this.enabled = true;
    }

    track(solver: GasSolver): void {
        const uniforms = this.marchUniforms;

        uniforms.uScalars.value = solver.scalars;
        uniforms.uLighting.value = solver.lighting;
        uniforms.uCoordinatesA.value = solver.coordinatesA;
        uniforms.uCoordinatesB.value = solver.coordinatesB;
        uniforms.uFlowWeight.value = solver.flowWeight;
    }

    forgetNoise(): void {
        this.noise?.dispose();
        this.noise = null;
    }

    fade(visibleShare: number): void {
        this.marchUniforms.uFade.value = visibleShare;
    }

    hide(): void {
        this.enabled = false;
    }

    override setDepthTexture(depthTexture: Texture): void {
        this.marchUniforms.uDepth.value = depthTexture;
    }

    override setSize(width: number, height: number): void {
        this.marchTarget.setSize(Math.max(Math.ceil(width * MARCH_RESOLUTION_SCALE), 1), Math.max(Math.ceil(height * MARCH_RESOLUTION_SCALE), 1));
    }

    override render(renderer: WebGLRenderer, inputBuffer: WebGLRenderTarget | null, outputBuffer: WebGLRenderTarget | null): void {
        const uniforms = this.marchUniforms;

        this.noise ??= renderDetailNoise(renderer);
        uniforms.uNoise.value = this.noise.texture;
        uniforms.uProjectionInverse.value = this.viewCamera.projectionMatrixInverse;
        uniforms.uCameraWorld.value = this.viewCamera.matrixWorld;
        uniforms.uCameraNear.value = this.viewCamera.near;
        uniforms.uCameraFar.value = this.viewCamera.far;
        this.fullscreenMaterial = this.march;
        renderer.setRenderTarget(this.marchTarget);
        renderer.render(this.scene, this.camera);

        this.composite.uniforms['inputBuffer'].value = inputBuffer?.texture ?? null;
        this.fullscreenMaterial = this.composite;
        renderer.setRenderTarget(this.renderToScreen ? null : outputBuffer);
        renderer.render(this.scene, this.camera);
    }
}

function marchUniformsOf(): MarchUniforms {
    return {
        uScalars: { value: null },
        uLighting: { value: null },
        uCoordinatesA: { value: null },
        uCoordinatesB: { value: null },
        uFlowWeight: { value: 0 },
        uNoise: { value: null },
        uDepth: { value: null },
        uProjectionInverse: { value: new Matrix4() },
        uCameraWorld: { value: new Matrix4() },
        uCameraNear: { value: 0.1 },
        uCameraFar: { value: 200 },
        uKeyLight: { value: new Color(1, 0.95, 0.88).multiplyScalar(KEY_LIGHT_SHARE * LIT_WHITE_LUMINANCE) },
        uSkyLight: { value: new Color(0.94, 0.98, 1.06).multiplyScalar(SKY_LIGHT_SHARE * LIT_WHITE_LUMINANCE) },
        uFlameLight: { value: new Color() },
        uCloudAlbedo: { value: new Color(1, 1, 1) },
        uFade: { value: 1 }
    };
}
