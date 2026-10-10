import { AdditiveBlending, BufferAttribute, DoubleSide, InstancedBufferGeometry, IUniform, Mesh, ShaderMaterial, Texture, Vector2, WebGLRenderer } from "three";
import { Disposable } from "../core/disposal-scope";
import { incandescenceTexture, IncandescenceUniforms, incandescenceUniformsOf } from "../rendering/incandescence-texture";
import { SparkField } from "./spark-field";
import { SPARK_STREAK_FRAGMENT_SHADER, SPARK_STREAK_VERTEX_SHADER } from "./spark-shaders";

interface StreakUniforms extends IncandescenceUniforms {
    readonly uPlaces: IUniform<Texture | null>;
    readonly uMotions: IUniform<Texture | null>;
    readonly uLaunchTraits: IUniform<Texture | null>;
    readonly uResolution: IUniform<Vector2>;
    readonly uPixelRatio: IUniform<number>;
    readonly uShutterSeconds: IUniform<number>;
    readonly uBrightness: IUniform<number>;
}

const STREAK_CORNERS = new Float32Array([-0.5, 0, 0, 0.5, 0, 0, 0.5, 1, 0, -0.5, 1, 0]);
const STREAK_TRIANGLES = [0, 1, 2, 0, 2, 3];
const SHUTTER_SECONDS = 1 / 45;
const SPARK_BRIGHTNESS = 2.2;

export class SparkStreaks implements Disposable {
    readonly mesh: Mesh;
    readonly material: ShaderMaterial;

    private readonly geometry = new InstancedBufferGeometry();
    private readonly incandescence = incandescenceTexture();
    private readonly uniforms: StreakUniforms;

    constructor() {
        this.uniforms = {
            uPlaces: { value: null },
            uMotions: { value: null },
            uLaunchTraits: { value: null },
            ...incandescenceUniformsOf(this.incandescence),
            uResolution: { value: new Vector2(1, 1) },
            uPixelRatio: { value: 1 },
            uShutterSeconds: { value: SHUTTER_SECONDS },
            uBrightness: { value: SPARK_BRIGHTNESS }
        };
        this.material = new ShaderMaterial({
            vertexShader: SPARK_STREAK_VERTEX_SHADER,
            fragmentShader: SPARK_STREAK_FRAGMENT_SHADER,
            uniforms: { ...this.uniforms },
            transparent: true,
            depthWrite: false,
            side: DoubleSide,
            blending: AdditiveBlending
        });
        this.geometry.setAttribute('position', new BufferAttribute(STREAK_CORNERS, 3));
        this.geometry.setIndex(STREAK_TRIANGLES);
        this.geometry.instanceCount = 0;
        this.mesh = new Mesh(this.geometry, this.material);
        this.mesh.name = 'spark-streaks';
        this.mesh.frustumCulled = false;
        this.mesh.onBeforeRender = (renderer) => this.fitTo(renderer);
    }

    show(field: SparkField, count: number): void {
        const { uniforms } = this;

        uniforms.uPlaces.value = field.places;
        uniforms.uMotions.value = field.motions;
        uniforms.uLaunchTraits.value = field.launchTraits;
        this.geometry.instanceCount = count;
    }

    hide(): void {
        this.geometry.instanceCount = 0;
    }

    dispose(): void {
        this.mesh.removeFromParent();
        this.geometry.dispose();
        this.material.dispose();
        this.incandescence.dispose();
    }

    private fitTo(renderer: WebGLRenderer): void {
        const target = renderer.getRenderTarget();
        const resolution = this.uniforms.uResolution.value;

        if (target === null) {
            renderer.getDrawingBufferSize(resolution);
        } else {
            resolution.set(target.width, target.height);
        }

        this.uniforms.uPixelRatio.value = renderer.getPixelRatio();
    }
}
