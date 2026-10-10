import { GLSL3, IUniform, RawShaderMaterial, Texture, Vector2, Vector3, Vector4 } from "three";
import { GAS_PASS_HEADER, GAS_PASS_VERTEX_SHADER, MAX_GAS_OBSTACLES, MAX_GAS_SOURCES } from "./gas-grid";
import { ADVECT_COORDINATES_SHADER, ADVECT_SCALARS_SHADER, ADVECT_VELOCITY_SHADER, ESTIMATE_SCALARS_SHADER } from "./gas-advection-shaders";
import { CURL_SHADER, DIVERGENCE_SHADER, FORCES_SHADER, JACOBI_SHADER, PROJECT_SHADER } from "./gas-projection-shaders";
import { GAS_LIGHTING_SHADER } from "./gas-lighting-shader";
import { KEY_LIGHT_POSITION } from "../rendering/lighting-rig";
import { GasLook } from "./gas-look";

export interface GasGridUniforms {
    readonly uVoxelsPerSide: IUniform<number>;
    readonly uTiles: IUniform<Vector2>;
    readonly uAtlas: IUniform<Vector2>;
}

export interface GasBoxUniforms {
    readonly uOrigin: IUniform<Vector3>;
    readonly uVoxelSize: IUniform<number>;
}

export interface GasSourceUniforms {
    readonly uSourcePlaces: IUniform<Vector4[]>;
    readonly uSourceFeeds: IUniform<Vector4[]>;
    readonly uSourceParticles: IUniform<number[]>;
    readonly uSourceCount: IUniform<number>;
}

export interface GasObstacleUniforms {
    readonly uObstaclePlaces: IUniform<Vector4[]>;
    readonly uObstacleVelocities: IUniform<Vector3[]>;
    readonly uObstacleCount: IUniform<number>;
}

export interface GasPhysicsUniforms {
    readonly uBuoyancyPerKelvin: IUniform<number>;
    readonly uSinkPerParticle: IUniform<number>;
    readonly uBurstPerFlame: IUniform<number>;
    readonly uVorticity: IUniform<number>;
}

export interface GasMistUniforms {
    readonly uCondensationKelvin: IUniform<Vector2>;
    readonly uMistExtinction: IUniform<number>;
    readonly uLightDirection: IUniform<Vector3>;
}

export interface SharedGasUniforms {
    readonly grid: GasGridUniforms;
    readonly step: { readonly uDt: IUniform<number> };
    readonly keep: { readonly uScalarKeep: IUniform<Vector4> };
    readonly box: GasBoxUniforms;
    readonly sources: GasSourceUniforms;
    readonly obstacles: GasObstacleUniforms;
    readonly physics: GasPhysicsUniforms;
    readonly mist: GasMistUniforms;
}

export interface GasPasses {
    readonly estimateScalars: RawShaderMaterial;
    readonly advectScalars: RawShaderMaterial;
    readonly advectVelocity: RawShaderMaterial;
    readonly curl: RawShaderMaterial;
    readonly forces: RawShaderMaterial;
    readonly divergence: RawShaderMaterial;
    readonly jacobi: RawShaderMaterial;
    readonly project: RawShaderMaterial;
    readonly lighting: RawShaderMaterial;
    readonly advectCoordinates: RawShaderMaterial;
}

const BUOYANCY_PER_KELVIN = 0.005;
const BURST_PER_FLAME = 1.5;
const VORTICITY = 1.8;
const VELOCITY_KEEP_PER_SECOND = 0.8;
const HEAT_KEEP_PER_SECOND = 0.03;
const VAPOR_KEEP_PER_SECOND = 0.6;
const FLAME_KEEP_PER_SECOND = 0.005;
const MIST_EXTINCTION_PER_ANGSTROM = 1.5;
const CONDENSATION_KELVIN = new Vector2(75, 45);

export function sharedGasUniformsOf(): SharedGasUniforms {
    return {
        grid: { uVoxelsPerSide: { value: 1 }, uTiles: { value: new Vector2(1, 1) }, uAtlas: { value: new Vector2(1, 1) } },
        step: { uDt: { value: 1 / 60 } },
        keep: { uScalarKeep: { value: new Vector4(HEAT_KEEP_PER_SECOND, VAPOR_KEEP_PER_SECOND, FLAME_KEEP_PER_SECOND, 1) } },
        box: { uOrigin: { value: new Vector3() }, uVoxelSize: { value: 1 } },
        sources: {
            uSourcePlaces: { value: Array.from({ length: MAX_GAS_SOURCES }, () => new Vector4()) },
            uSourceFeeds: { value: Array.from({ length: MAX_GAS_SOURCES }, () => new Vector4()) },
            uSourceParticles: { value: new Array<number>(MAX_GAS_SOURCES).fill(0) },
            uSourceCount: { value: 0 }
        },
        obstacles: {
            uObstaclePlaces: { value: Array.from({ length: MAX_GAS_OBSTACLES }, () => new Vector4()) },
            uObstacleVelocities: { value: Array.from({ length: MAX_GAS_OBSTACLES }, () => new Vector3()) },
            uObstacleCount: { value: 0 }
        },
        physics: { uBuoyancyPerKelvin: { value: 0 }, uSinkPerParticle: { value: 0 }, uBurstPerFlame: { value: 0 }, uVorticity: { value: VORTICITY } },
        mist: { uCondensationKelvin: { value: CONDENSATION_KELVIN }, uMistExtinction: { value: MIST_EXTINCTION_PER_ANGSTROM }, uLightDirection: { value: new Vector3(...KEY_LIGHT_POSITION).normalize() } }
    };
}

export function sizeGasGrid({ grid }: SharedGasUniforms, voxelsPerSide: number): Vector2 {
    const tilesX = Math.ceil(Math.sqrt(voxelsPerSide));
    const tilesY = Math.ceil(voxelsPerSide / tilesX);

    grid.uVoxelsPerSide.value = voxelsPerSide;
    grid.uTiles.value.set(tilesX, tilesY);

    return grid.uAtlas.value.set(tilesX * voxelsPerSide, tilesY * voxelsPerSide);
}

export function placeGasBox({ box, physics, keep }: SharedGasUniforms, origin: Vector3, voxelSize: number, look: GasLook): void {
    box.uOrigin.value.copy(origin);
    box.uVoxelSize.value = voxelSize;
    physics.uBuoyancyPerKelvin.value = BUOYANCY_PER_KELVIN / voxelSize;
    physics.uSinkPerParticle.value = look.particleSinkPerDensity / voxelSize;
    physics.uBurstPerFlame.value = BURST_PER_FLAME / voxelSize;
    keep.uScalarKeep.value.w = look.particleKeepPerSecond;
}

export function gasPassesOf({ grid, step, keep, box, sources, obstacles, physics, mist }: SharedGasUniforms): GasPasses {
    return {
        estimateScalars: passOf(ESTIMATE_SCALARS_SHADER, { ...grid, ...step, ...inputsOf('uVelocity', 'uScalars') }),
        advectScalars: passOf(ADVECT_SCALARS_SHADER, { ...grid, ...step, ...keep, ...sources, ...inputsOf('uVelocity', 'uScalars', 'uEstimate') }),
        advectVelocity: passOf(ADVECT_VELOCITY_SHADER, { ...grid, ...step, ...inputsOf('uVelocity'), uVelocityKeep: { value: VELOCITY_KEEP_PER_SECOND } }),
        curl: passOf(CURL_SHADER, { ...grid, ...inputsOf('uVelocity') }),
        forces: passOf(FORCES_SHADER, { ...grid, ...step, ...sources, ...obstacles, ...physics, ...inputsOf('uVelocity', 'uCurl', 'uScalars') }),
        divergence: passOf(DIVERGENCE_SHADER, { ...grid, ...inputsOf('uVelocity') }),
        jacobi: passOf(JACOBI_SHADER, { ...grid, ...inputsOf('uPressure', 'uDivergence') }),
        project: passOf(PROJECT_SHADER, { ...grid, ...obstacles, ...inputsOf('uVelocity', 'uPressure') }),
        lighting: passOf(GAS_LIGHTING_SHADER, { ...grid, ...mist, uVoxelSize: box.uVoxelSize, ...inputsOf('uScalars') }),
        advectCoordinates: passOf(ADVECT_COORDINATES_SHADER, { ...grid, ...step, ...box, ...inputsOf('uVelocity', 'uCoordinates'), uReset: { value: 0 } })
    };
}

function inputsOf(...names: readonly string[]): Record<string, IUniform<Texture | null>> {
    return Object.fromEntries(names.map((name) => [name, { value: null }]));
}

function passOf(body: string, uniforms: Record<string, IUniform>): RawShaderMaterial {
    return new RawShaderMaterial({
        glslVersion: GLSL3,
        vertexShader: GAS_PASS_VERTEX_SHADER,
        fragmentShader: GAS_PASS_HEADER + body,
        uniforms,
        depthTest: false,
        depthWrite: false
    });
}
