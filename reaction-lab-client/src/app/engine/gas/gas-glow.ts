import { Color, FloatType, GLSL3, IUniform, Material, NearestFilter, RawShaderMaterial, Texture, Vector2, Vector3, WebGLRenderer, WebGLRenderTarget } from "three";
import { Disposable } from "../core/disposal-scope";
import { ScreenQuad } from "../rendering/screen-quad";
import { GAS_PASS_HEADER, GAS_PASS_VERTEX_SHADER } from "./gas-grid";
import { GasSolver } from "./gas-solver";
import { emissivityOf, flameLightOf, GasLook } from "./gas-look";
import { GasParticles } from "./gas-run";
import { GLOW_BLOCKS_PER_SIDE, GLOW_CELL_VOXELS, GLOW_CELLS_SHADER, GLOW_GROUP_CELLS, GLOW_REDUCE_SHADER } from "./gas-glow-shaders";
import { incandescenceTexture, incandescenceUniformsOf, writeIncandescence } from "../rendering/incandescence-texture";
import { GlowSource } from "../rendering/reaction-light";
import { luminanceOf } from "../rendering/luminance";

interface GridLayout {
    readonly side: number;
    readonly tilesX: number;
    readonly width: number;
    readonly height: number;
}

interface MeasuredBox {
    readonly origin: Vector3;
    readonly sideAngstrom: number;
    readonly visibleShare: number;
}

interface GlowRequest {
    readonly solver: GasSolver;
    readonly visibleShare: number;
}

const BLOCK_LAYOUT = layoutOf(GLOW_BLOCKS_PER_SIDE);
const EQUAL_VOLUME_RADIUS_SHARE = Math.cbrt(3 / (4 * Math.PI));
const DIMMEST_LUMINANCE = 1e-3;
const WHITE_ALBEDO = new Color(1, 1, 1);

export class GasGlow implements Disposable {
    private readonly cells = glowTarget();
    private readonly groups = glowTarget();
    private readonly blocks = glowTarget();
    private readonly incandescence = incandescenceTexture();
    private readonly cellPass = glowPass(GLOW_CELLS_SHADER, { ...incandescenceUniformsOf(this.incandescence), uScalars: { value: null }, uFlameLight: { value: new Color() }, uParticleAbsorption: { value: 0 } });
    private readonly reducePass = glowPass(GLOW_REDUCE_SHADER, { uSource: { value: null }, uSourceGrid: { value: new Vector2() } });
    private readonly quad: ScreenQuad;
    private readonly texels = new Float32Array(BLOCK_LAYOUT.width * BLOCK_LAYOUT.height * 4);
    private waiting: GlowRequest | null = null;
    private pending = false;
    private arrived = false;
    private generation = 0;

    constructor(private readonly renderer: WebGLRenderer, private readonly onGlow: (sources: readonly GlowSource[]) => void) {
        this.quad = new ScreenQuad(renderer);
        this.blocks.setSize(BLOCK_LAYOUT.width, BLOCK_LAYOUT.height);
    }

    get materials(): readonly Material[] {
        return [this.cellPass, this.reducePass];
    }

    prepare(look: GasLook, particles: GasParticles | null): void {
        const emissivity = emissivityOf(particles?.albedo ?? WHITE_ALBEDO);

        writeIncandescence(this.incandescence, emissivity);
        flameLightOf(look, this.cellPass.uniforms['uFlameLight'].value);
        this.cellPass.uniforms['uParticleAbsorption'].value = emissivity;
        this.forget();
    }

    measure(solver: GasSolver, visibleShare: number): void {
        this.waiting = { solver, visibleShare };
        this.startWaiting();
    }

    poll(): boolean {
        const arrived = this.arrived;

        this.startWaiting();
        this.arrived = false;

        return arrived;
    }

    forget(): void {
        this.generation++;
        this.waiting = null;
        this.arrived = true;
        this.onGlow([]);
    }

    dispose(): void {
        this.generation++;

        for (const target of [this.cells, this.groups, this.blocks]) {
            target.dispose();
        }

        this.incandescence.dispose();
        this.cellPass.dispose();
        this.reducePass.dispose();
        this.quad.dispose();
    }

    private startWaiting(): void {
        if (this.pending || this.waiting === null) {
            return;
        }

        const { solver, visibleShare } = this.waiting;
        const voxelsPerSide = solver.grid.uVoxelsPerSide.value;
        const cellLayout = layoutOf(voxelsPerSide / GLOW_CELL_VOXELS);
        const groupLayout = layoutOf(cellLayout.side / GLOW_GROUP_CELLS);
        const measured: MeasuredBox = { origin: solver.box.uOrigin.value.clone(), sideAngstrom: solver.box.uVoxelSize.value * voxelsPerSide, visibleShare };
        const generation = this.generation;

        this.waiting = null;
        Object.assign(this.cellPass.uniforms, solver.grid, solver.mist, { uVoxelSize: solver.box.uVoxelSize });
        this.cellPass.uniforms['uScalars'].value = solver.scalars;
        this.cellPass.uniforms['uDestination'].value.set(cellLayout.side, cellLayout.tilesX);
        this.draw(this.cellPass, this.cells, cellLayout);
        this.reduce(this.cells.texture, cellLayout, this.groups, groupLayout);
        this.reduce(this.groups.texture, groupLayout, this.blocks, BLOCK_LAYOUT);
        this.pending = true;
        this.renderer.readRenderTargetPixelsAsync(this.blocks, 0, 0, BLOCK_LAYOUT.width, BLOCK_LAYOUT.height, this.texels)
            .then(() => this.settle(generation, measured))
            .catch(() => this.settle(generation, null));
    }

    private settle(generation: number, measured: MeasuredBox | null): void {
        this.pending = false;

        if (measured !== null && generation === this.generation) {
            this.onGlow(this.sourcesOf(measured));
            this.arrived = true;
        }
    }

    private sourcesOf({ origin, sideAngstrom, visibleShare }: MeasuredBox): GlowSource[] {
        const blockSide = sideAngstrom / GLOW_BLOCKS_PER_SIDE;
        const radius = blockSide * EQUAL_VOLUME_RADIUS_SHARE;
        const sources: GlowSource[] = [];

        for (let index = 0; index < GLOW_BLOCKS_PER_SIDE ** 3; index++) {
            const block = new Vector3(index % GLOW_BLOCKS_PER_SIDE, Math.floor(index / GLOW_BLOCKS_PER_SIDE) % GLOW_BLOCKS_PER_SIDE, Math.floor(index / GLOW_BLOCKS_PER_SIDE ** 2));
            const offset = 4 * texelIndexOf(block, BLOCK_LAYOUT);
            const absorptionDepth = (this.texels[offset + 3] / blockSide ** 3) * radius;
            const radiance = new Color().fromArray(this.texels, offset).multiplyScalar(visibleShare / (Math.PI * radius ** 2 * (1 + (4 / 3) * absorptionDepth)));

            if (luminanceOf(radiance) * radius ** 2 > DIMMEST_LUMINANCE) {
                sources.push({ place: block.addScalar(0.5).multiplyScalar(blockSide).add(origin), radius, radiance });
            }
        }

        return sources;
    }

    private reduce(source: Texture, sourceLayout: GridLayout, target: WebGLRenderTarget, layout: GridLayout): void {
        const { uniforms } = this.reducePass;

        uniforms['uSource'].value = source;
        uniforms['uSourceGrid'].value.set(sourceLayout.side, sourceLayout.tilesX);
        uniforms['uDestination'].value.set(layout.side, layout.tilesX);
        this.draw(this.reducePass, target, layout);
    }

    private draw(material: RawShaderMaterial, target: WebGLRenderTarget, layout: GridLayout): void {
        if (target.width !== layout.width || target.height !== layout.height) {
            target.setSize(layout.width, layout.height);
        }

        this.quad.draw(material, target);
    }
}

function layoutOf(side: number): GridLayout {
    const tilesX = Math.ceil(Math.sqrt(side));

    return { side, tilesX, width: tilesX * side, height: Math.ceil(side / tilesX) * side };
}

function texelIndexOf(block: Vector3, layout: GridLayout): number {
    return (Math.floor(block.z / layout.tilesX) * layout.side + block.y) * layout.width + (block.z % layout.tilesX) * layout.side + block.x;
}

function glowTarget(): WebGLRenderTarget {
    return new WebGLRenderTarget(1, 1, { type: FloatType, minFilter: NearestFilter, magFilter: NearestFilter, depthBuffer: false });
}

function glowPass(body: string, uniforms: Record<string, IUniform>): RawShaderMaterial {
    return new RawShaderMaterial({
        glslVersion: GLSL3,
        vertexShader: GAS_PASS_VERTEX_SHADER,
        fragmentShader: GAS_PASS_HEADER + body,
        uniforms: { uVoxelsPerSide: { value: 1 }, uTiles: { value: new Vector2(1, 1) }, uAtlas: { value: new Vector2(1, 1) }, uDestination: { value: new Vector2(1, 1) }, ...uniforms },
        depthTest: false,
        depthWrite: false
    });
}
