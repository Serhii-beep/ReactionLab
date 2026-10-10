import { BufferAttribute, BufferGeometry, Camera, DataTexture, FloatType, GLSL3, IUniform, Mesh, NearestFilter, RawShaderMaterial, RGBAFormat, Texture, Vector2, Vector3, WebGLRenderer, WebGLRenderTarget } from "three";
import { Disposable } from "../core/disposal-scope";
import { ROOM_TEMPERATURE_KELVIN } from "../core/matter";
import { GAS_PASS_VERTEX_SHADER } from "../gas/gas-grid";
import { GasAirflow } from "../gas/gas-airflow";
import { SPARK_SIDE, SPARK_SLOTS, SparkLaunch } from "./spark-launch";
import { SPARK_STEP_SHADER } from "./spark-shaders";

interface StepUniforms {
    readonly uPlaces: IUniform<Texture | null>;
    readonly uMotions: IUniform<Texture | null>;
    readonly uLaunchPlaces: IUniform<Texture>;
    readonly uLaunchMotions: IUniform<Texture>;
    readonly uLaunchTraits: IUniform<Texture>;
    readonly uAirVelocity: IUniform<Texture | null>;
    readonly uAirScalars: IUniform<Texture | null>;
    readonly uAirflow: IUniform<number>;
    readonly uOrigin: IUniform<Vector3>;
    readonly uVoxelSize: IUniform<number>;
    readonly uVoxelsPerSide: IUniform<number>;
    readonly uTiles: IUniform<Vector2>;
    readonly uAtlas: IUniform<Vector2>;
    readonly uSeconds: IUniform<number>;
    readonly uDt: IUniform<number>;
    readonly uRoomKelvin: IUniform<number>;
}

const UNBORN_SECONDS = 1e9;
const BEFORE_EVERY_LAUNCH_SECONDS = -1e9;
const CHANNELS = 4;

export class SparkField implements Disposable {
    private readonly launchPlaceTexels = new Float32Array(SPARK_SLOTS * CHANNELS);
    private readonly launchMotionTexels = new Float32Array(SPARK_SLOTS * CHANNELS);
    private readonly launchTraitTexels = new Float32Array(SPARK_SLOTS * CHANNELS);
    private readonly launchPlaces = launchTexture(this.launchPlaceTexels);
    private readonly launchMotions = launchTexture(this.launchMotionTexels);
    private readonly traits = launchTexture(this.launchTraitTexels);
    private readonly uniforms: StepUniforms;
    private readonly material: RawShaderMaterial;
    private readonly quad = new Mesh(new BufferGeometry());
    private readonly camera = new Camera();
    private states: readonly [WebGLRenderTarget, WebGLRenderTarget] = [stateTarget(), stateTarget()];

    constructor(private readonly renderer: WebGLRenderer) {
        this.uniforms = stepUniformsOf(this.launchPlaces, this.launchMotions, this.traits);
        this.material = new RawShaderMaterial({
            glslVersion: GLSL3,
            vertexShader: GAS_PASS_VERTEX_SHADER,
            fragmentShader: SPARK_STEP_SHADER,
            uniforms: { ...this.uniforms },
            depthTest: false,
            depthWrite: false
        });
        this.quad.geometry.setAttribute('position', new BufferAttribute(new Float32Array([-1, -1, 0, 3, -1, 0, -1, 3, 0]), 3));
        this.quad.material = this.material;
        this.quad.frustumCulled = false;
    }

    get places(): Texture {
        return this.states[0].textures[0];
    }

    get motions(): Texture {
        return this.states[0].textures[1];
    }

    get launchTraits(): Texture {
        return this.traits;
    }

    load(launches: readonly SparkLaunch[]): void {
        for (let slot = 0; slot < SPARK_SLOTS; slot++) {
            this.loadSlot(slot, launches[slot]);
        }

        for (const texture of [this.launchPlaces, this.launchMotions, this.traits]) {
            texture.needsUpdate = true;
        }
    }

    reset(): void {
        this.draw(BEFORE_EVERY_LAUNCH_SECONDS, 0, null);
    }

    step(seconds: number, stepSeconds: number, airflow: GasAirflow | null): void {
        this.draw(seconds, stepSeconds, airflow);
    }

    dispose(): void {
        this.quad.geometry.dispose();
        this.material.dispose();

        for (const state of this.states) {
            state.dispose();
        }

        for (const texture of [this.launchPlaces, this.launchMotions, this.traits]) {
            texture.dispose();
        }
    }

    private loadSlot(slot: number, launch: SparkLaunch | undefined): void {
        const offset = slot * CHANNELS;

        if (launch === undefined) {
            this.launchPlaceTexels.fill(0, offset, offset + CHANNELS).fill(UNBORN_SECONDS, offset + 3, offset + CHANNELS);
            this.launchMotionTexels.fill(0, offset, offset + CHANNELS);
            this.launchTraitTexels.fill(0, offset, offset + CHANNELS);

            return;
        }

        this.launchPlaceTexels.set([launch.place.x, launch.place.y, launch.place.z, launch.seconds], offset);
        this.launchMotionTexels.set([launch.velocity.x, launch.velocity.y, launch.velocity.z, launch.kelvin], offset);
        this.launchTraitTexels.set([launch.dragPerSecond, launch.liftAngstromPerSecondSquared, launch.coolingSeconds, launch.widthPixels], offset);
    }

    private draw(seconds: number, stepSeconds: number, airflow: GasAirflow | null): void {
        const { uniforms, renderer } = this;
        const previousTarget = renderer.getRenderTarget();

        uniforms.uPlaces.value = this.places;
        uniforms.uMotions.value = this.motions;
        uniforms.uSeconds.value = seconds;
        uniforms.uDt.value = stepSeconds;
        this.ride(airflow);
        renderer.setRenderTarget(this.states[1]);
        renderer.render(this.quad, this.camera);
        renderer.setRenderTarget(previousTarget);
        this.states = [this.states[1], this.states[0]];
    }

    private ride(airflow: GasAirflow | null): void {
        const { uniforms } = this;

        uniforms.uAirflow.value = airflow === null ? 0 : 1;
        uniforms.uAirVelocity.value = airflow?.velocity ?? null;
        uniforms.uAirScalars.value = airflow?.scalars ?? null;

        if (airflow === null) {
            return;
        }

        uniforms.uOrigin.value.copy(airflow.box.uOrigin.value);
        uniforms.uVoxelSize.value = airflow.box.uVoxelSize.value;
        uniforms.uVoxelsPerSide.value = airflow.grid.uVoxelsPerSide.value;
        uniforms.uTiles.value.copy(airflow.grid.uTiles.value);
        uniforms.uAtlas.value.copy(airflow.grid.uAtlas.value);
    }
}

function stepUniformsOf(launchPlaces: Texture, launchMotions: Texture, launchTraits: Texture): StepUniforms {
    return {
        uPlaces: { value: null },
        uMotions: { value: null },
        uLaunchPlaces: { value: launchPlaces },
        uLaunchMotions: { value: launchMotions },
        uLaunchTraits: { value: launchTraits },
        uAirVelocity: { value: null },
        uAirScalars: { value: null },
        uAirflow: { value: 0 },
        uOrigin: { value: new Vector3() },
        uVoxelSize: { value: 1 },
        uVoxelsPerSide: { value: 1 },
        uTiles: { value: new Vector2(1, 1) },
        uAtlas: { value: new Vector2(1, 1) },
        uSeconds: { value: 0 },
        uDt: { value: 0 },
        uRoomKelvin: { value: ROOM_TEMPERATURE_KELVIN }
    };
}

function launchTexture(data: Float32Array): DataTexture {
    const texture = new DataTexture(data, SPARK_SIDE, SPARK_SIDE, RGBAFormat, FloatType);

    texture.minFilter = NearestFilter;
    texture.magFilter = NearestFilter;
    texture.needsUpdate = true;

    return texture;
}

function stateTarget(): WebGLRenderTarget {
    return new WebGLRenderTarget(SPARK_SIDE, SPARK_SIDE, {
        count: 2,
        type: FloatType,
        format: RGBAFormat,
        minFilter: NearestFilter,
        magFilter: NearestFilter,
        depthBuffer: false,
        stencilBuffer: false,
        generateMipmaps: false
    });
}
