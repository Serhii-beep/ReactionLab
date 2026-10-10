import { BufferAttribute, BufferGeometry, Camera, Color, Mesh, RawShaderMaterial, Texture, Vector3, Vector4, WebGLRenderer, WebGLRenderTarget } from "three";
import { Disposable } from "../core/disposal-scope";
import { MAX_GAS_OBSTACLES, MAX_GAS_SOURCES } from "./gas-grid";
import { GasFields } from "./gas-fields";
import { FieldPair } from "./field-pair";
import { GasObstacle, GasSource } from "./gas-run";
import { GasBoxUniforms, GasGridUniforms, GasMistUniforms, gasPassesOf, GasPasses, placeGasBox, sharedGasUniformsOf, SharedGasUniforms, sizeGasGrid } from "./gas-passes";

type TextureByUniformName = Readonly<Record<string, Texture>>;

const PRESSURE_ITERATIONS = 24;
const FLOW_PERIOD_SECONDS = 1.1;
const SMALLEST_SOURCE_VOXELS = 0.75;

export class GasSolver implements Disposable {
    private readonly fields = new GasFields();
    private readonly shared: SharedGasUniforms = sharedGasUniformsOf();
    private readonly passes: GasPasses = gasPassesOf(this.shared);
    private readonly quad = new Mesh(new BufferGeometry());
    private readonly camera = new Camera();
    private flowSeconds = 0;

    constructor(private readonly renderer: WebGLRenderer) {
        this.quad.geometry.setAttribute('position', new BufferAttribute(new Float32Array([-1, -1, 0, 3, -1, 0, -1, 3, 0]), 3));
        this.quad.frustumCulled = false;
    }

    get grid(): GasGridUniforms {
        return this.shared.grid;
    }

    get box(): GasBoxUniforms {
        return this.shared.box;
    }

    get mist(): GasMistUniforms {
        return this.shared.mist;
    }

    get velocity(): Texture {
        return this.fields.velocity.read.texture;
    }

    get scalars(): Texture {
        return this.fields.scalars.read.texture;
    }

    get lighting(): Texture {
        return this.fields.lighting.texture;
    }

    get coordinatesA(): Texture {
        return this.fields.coordinatesA.read.texture;
    }

    get coordinatesB(): Texture {
        return this.fields.coordinatesB.read.texture;
    }

    get flowWeight(): number {
        return 1 - Math.abs(2 * ((this.flowSeconds / FLOW_PERIOD_SECONDS) % 1) - 1);
    }

    allocate(voxelsPerSide: number): void {
        const atlas = sizeGasGrid(this.shared, voxelsPerSide);

        this.fields.setSize(atlas.x, atlas.y);
    }

    place(origin: Vector3, sideAngstrom: number): void {
        placeGasBox(this.shared, origin, sideAngstrom / this.shared.grid.uVoxelsPerSide.value);
        this.clear();
    }

    setSources(sources: readonly GasSource[]): void {
        const { uSourcePlaces, uSourceFeeds, uSourceClouds, uSourceCount } = this.shared.sources;
        const count = Math.min(sources.length, MAX_GAS_SOURCES);

        for (let index = 0; index < count; index++) {
            const source = sources[index];

            this.toVoxels(source.place, uSourcePlaces.value[index]).setW(Math.max(source.radius / this.box.uVoxelSize.value, SMALLEST_SOURCE_VOXELS));
            uSourceFeeds.value[index].set(source.targetKelvin, source.heatingPerSecond, source.vaporPerSecond, source.flamePerSecond);
            uSourceClouds.value[index] = source.cloudPerSecond;
        }

        uSourceCount.value = count;
    }

    setObstacles(obstacles: readonly GasObstacle[]): void {
        const { uObstaclePlaces, uObstacleVelocities, uObstacleCount } = this.shared.obstacles;
        const count = Math.min(obstacles.length, MAX_GAS_OBSTACLES);

        for (let index = 0; index < count; index++) {
            const obstacle = obstacles[index];

            this.toVoxels(obstacle.place, uObstaclePlaces.value[index]).setW(obstacle.radius / this.box.uVoxelSize.value);
            uObstacleVelocities.value[index].copy(obstacle.velocity).divideScalar(this.box.uVoxelSize.value);
        }

        uObstacleCount.value = count;
    }

    step(seconds: number): void {
        const { fields, passes } = this;

        this.shared.step.uDt.value = seconds;
        this.drawing(() => {
            this.draw(passes.estimateScalars, { uVelocity: fields.velocity.read.texture, uScalars: fields.scalars.read.texture }, fields.estimate);
            this.drawPair(passes.advectScalars, { uVelocity: fields.velocity.read.texture, uScalars: fields.scalars.read.texture, uEstimate: fields.estimate.texture }, fields.scalars);
            this.drawPair(passes.advectVelocity, { uVelocity: fields.velocity.read.texture }, fields.velocity);
            this.draw(passes.curl, { uVelocity: fields.velocity.read.texture }, fields.curl);
            this.drawPair(passes.forces, { uVelocity: fields.velocity.read.texture, uCurl: fields.curl.texture, uScalars: fields.scalars.read.texture }, fields.velocity);
            this.draw(passes.divergence, { uVelocity: fields.velocity.read.texture }, fields.divergence);

            for (let iteration = 0; iteration < PRESSURE_ITERATIONS; iteration++) {
                this.drawPair(passes.jacobi, { uPressure: fields.pressure.read.texture, uDivergence: fields.divergence.texture }, fields.pressure);
            }

            this.drawPair(passes.project, { uVelocity: fields.velocity.read.texture, uPressure: fields.pressure.read.texture }, fields.velocity);
            this.carryCoordinates(seconds);
        });
    }

    light(): void {
        this.drawing(() => this.draw(this.passes.lighting, { uScalars: this.fields.scalars.read.texture }, this.fields.lighting));
    }

    clear(): void {
        const renderer = this.renderer;
        const previousColor = renderer.getClearColor(new Color());
        const previousAlpha = renderer.getClearAlpha();

        this.flowSeconds = 0;
        renderer.setClearColor(0x000000, 0);
        this.drawing(() => {
            for (const target of this.fields.simulated) {
                renderer.setRenderTarget(target);
                renderer.clear(true, false, false);
            }

            for (const pair of [this.fields.coordinatesA, this.fields.coordinatesB, this.fields.coordinatesA, this.fields.coordinatesB]) {
                this.carry(pair, true);
            }
        });
        renderer.setClearColor(previousColor, previousAlpha);
    }

    release(): void {
        this.fields.dispose();
    }

    dispose(): void {
        this.fields.dispose();

        for (const pass of Object.values(this.passes)) {
            pass.dispose();
        }

        this.quad.geometry.dispose();
    }

    private drawing(work: () => void): void {
        const renderer = this.renderer;
        const previousTarget = renderer.getRenderTarget();
        const previousAutoClear = renderer.autoClear;

        renderer.autoClear = false;
        work();
        renderer.autoClear = previousAutoClear;
        renderer.setRenderTarget(previousTarget);
    }

    private carryCoordinates(seconds: number): void {
        const before = this.flowSeconds / FLOW_PERIOD_SECONDS;
        const after = (this.flowSeconds + seconds) / FLOW_PERIOD_SECONDS;

        this.flowSeconds += seconds;
        this.carry(this.fields.coordinatesA, Math.floor(after) > Math.floor(before));
        this.carry(this.fields.coordinatesB, Math.floor(after + 0.5) > Math.floor(before + 0.5));
    }

    private carry(pair: FieldPair, reset: boolean): void {
        this.passes.advectCoordinates.uniforms['uReset'].value = reset ? 1 : 0;
        this.drawPair(this.passes.advectCoordinates, { uVelocity: this.fields.velocity.read.texture, uCoordinates: pair.read.texture }, pair);
    }

    private drawPair(material: RawShaderMaterial, inputs: TextureByUniformName, pair: FieldPair): void {
        this.draw(material, inputs, pair.write);
        pair.swap();
    }

    private draw(material: RawShaderMaterial, inputs: TextureByUniformName, output: WebGLRenderTarget): void {
        for (const [name, texture] of Object.entries(inputs)) {
            material.uniforms[name].value = texture;
        }

        this.quad.material = material;
        this.renderer.setRenderTarget(output);
        this.renderer.render(this.quad, this.camera);
    }

    private toVoxels(world: Vector3, target: Vector4): Vector4 {
        const { uOrigin, uVoxelSize } = this.box;
        const origin = uOrigin.value;
        const scale = 1 / uVoxelSize.value;

        return target.set((world.x - origin.x) * scale, (world.y - origin.y) * scale, (world.z - origin.z) * scale, 0);
    }
}
