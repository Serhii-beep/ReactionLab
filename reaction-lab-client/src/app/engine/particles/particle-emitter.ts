import { AdditiveBlending, Blending, BufferAttribute, Color, InstancedBufferAttribute, InstancedBufferGeometry, IUniform, Mesh, NoBlending, NormalBlending, ShaderMaterial, Vector3 } from "three";
import { Disposable } from "../core/disposal-scope";
import { PARTICLE_LOOKS, ParticleLook } from "./particle-look";
import { anchorPointsOf, EmissionAnchors, EmissionPlan, hash01 } from "./emission-plan";
import { PARTICLE_FRAGMENT_SHADER, PARTICLE_VERTEX_SHADER } from "./particle-shader";

interface ParticleAttributes {
    readonly origins: Float32Array;
    readonly velocities: Float32Array;
    readonly lifecycles: Float32Array;
    readonly lastDeathSeconds: number;
}

interface Variation {
    readonly minimum: number;
    readonly range: number;
}

const QUAD_CORNERS = new Float32Array([-0.5, -0.5, 0, 0.5, -0.5, 0, 0.5, 0.5, 0, -0.5, 0.5, 0]);
const QUAD_TRIANGLES = [0, 1, 2, 0, 2, 3];
const NO_FLOOR = -1e6;
const PRECIPITATE_REST_ANGSTROM = 0.04;
const NEVER_FADES_AGE = 2;
const MIN_FADE_FRACTION = 0.001;
const MIN_DRAG_PER_SECOND = 0.0001;
const OPAQUE_ALPHA_CUTOFF = 0.5;
const MIN_PARTICLES = 1;
const SPEED_VARIATION: Variation = { minimum: 0.5, range: 1 };
const LIFE_VARIATION: Variation = { minimum: 0.7, range: 0.6 };
const SIZE_VARIATION: Variation = { minimum: 0.6, range: 0.8 };
const HASH_CHANNELS = { azimuth: 1, elevation: 2, radius: 3, speed: 4, life: 5, size: 6, seed: 7, birth: 8 };

export class ParticleEmitter implements Disposable {
    readonly mesh: Mesh;

    private readonly geometry: InstancedBufferGeometry;
    private readonly material: ShaderMaterial;
    private readonly elapsedUniform: IUniform<number> = { value: 0 };
    private readonly firstBirthSeconds: number;
    private readonly lastDeathSeconds: number;
    private readonly particleCount: number;

    constructor(plan: EmissionPlan, anchors: EmissionAnchors, budgetScale: number) {
        const look = PARTICLE_LOOKS[plan.kind];
        const particleCount = Math.max(MIN_PARTICLES, Math.round(plan.particleCount * budgetScale));
        const attributes = planParticles(plan, look, anchorPointsOf(anchors, plan.anchor), particleCount);

        this.geometry = particleGeometry(attributes, particleCount);
        this.material = particleMaterial(plan, look, this.elapsedUniform);
        this.mesh = new Mesh(this.geometry, this.material);
        this.mesh.frustumCulled = false;
        this.mesh.name = `particles-${plan.kind}`;
        this.geometry.instanceCount = 0;
        this.particleCount = particleCount;
        this.firstBirthSeconds = plan.startSeconds;
        this.lastDeathSeconds = attributes.lastDeathSeconds;
    }

    setElapsed(elapsedSeconds: number): boolean {
        const alive = elapsedSeconds >= this.firstBirthSeconds && elapsedSeconds <= this.lastDeathSeconds;

        this.elapsedUniform.value = elapsedSeconds;
        this.geometry.instanceCount = alive ? this.particleCount : 0;

        return alive;
    }

    dispose(): void {
        this.mesh.removeFromParent();
        this.geometry.dispose();
        this.material.dispose();
    }
}

function planParticles(plan: EmissionPlan, look: ParticleLook, anchorPoints: readonly Vector3[], particleCount: number): ParticleAttributes {
    const origins = new Float32Array(particleCount * 3);
    const velocities = new Float32Array(particleCount * 3);
    const lifecycles = new Float32Array(particleCount * 4);
    const direction = new Vector3();
    let lastDeathSeconds = plan.startSeconds;

    for (let index = 0; index < particleCount; index++) {
        const anchorPoint = anchorPoints[index % anchorPoints.length];
        const radius = plan.spreadAngstrom * Math.cbrt(hash01(index, HASH_CHANNELS.radius));
        const speed = look.speedAngstromPerSecond * plan.magnitude * vary(SPEED_VARIATION, hash01(index, HASH_CHANNELS.speed));
        const birthSeconds = plan.startSeconds + plan.windowSeconds * hash01(index, HASH_CHANNELS.birth);
        const lifeSeconds = plan.lifeSeconds * vary(LIFE_VARIATION, hash01(index, HASH_CHANNELS.life));
        const vertex = index * 3;
        const cycle = index * 4;

        randomDirection(hash01(index, HASH_CHANNELS.azimuth), hash01(index, HASH_CHANNELS.elevation), direction);
        origins[vertex] = anchorPoint.x + direction.x * radius;
        origins[vertex + 1] = anchorPoint.y + direction.y * radius;
        origins[vertex + 2] = anchorPoint.z + direction.z * radius;
        velocities[vertex] = direction.x * speed;
        velocities[vertex + 1] = direction.y * speed + look.liftAngstromPerSecond * plan.magnitude;
        velocities[vertex + 2] = direction.z * speed;
        lifecycles[cycle] = birthSeconds;
        lifecycles[cycle + 1] = lifeSeconds;
        lifecycles[cycle + 2] = look.sizeAngstrom * plan.magnitude * vary(SIZE_VARIATION, hash01(index, HASH_CHANNELS.size));
        lifecycles[cycle + 3] = hash01(index, HASH_CHANNELS.seed);
        lastDeathSeconds = Math.max(lastDeathSeconds, birthSeconds + lifeSeconds);
    }

    return { origins, velocities, lifecycles, lastDeathSeconds };
}

function particleGeometry(attributes: ParticleAttributes, particleCount: number): InstancedBufferGeometry {
    const geometry = new InstancedBufferGeometry();

    geometry.setAttribute('position', new BufferAttribute(QUAD_CORNERS, 3));
    geometry.setIndex(QUAD_TRIANGLES);
    geometry.setAttribute('origin', new InstancedBufferAttribute(attributes.origins, 3));
    geometry.setAttribute('velocity', new InstancedBufferAttribute(attributes.velocities, 3));
    geometry.setAttribute('lifecycle', new InstancedBufferAttribute(attributes.lifecycles, 4));
    geometry.instanceCount = particleCount;

    return geometry;
}

function particleMaterial(plan: EmissionPlan, look: ParticleLook, elapsedUniform: IUniform<number>): ShaderMaterial {
    const fadeOutStart = look.fadeOutLifeFraction > 0 ? 1 - look.fadeOutLifeFraction : NEVER_FADES_AGE;

    return new ShaderMaterial({
        vertexShader: PARTICLE_VERTEX_SHADER,
        fragmentShader: PARTICLE_FRAGMENT_SHADER,
        transparent: !look.opaque,
        depthWrite: look.opaque,
        blending: blendingOf(look),
        uniforms: {
            uElapsedSeconds: elapsedUniform,
            uGravity: { value: look.gravityAngstromPerSecondSquared.clone() },
            uGrowthPerLife: { value: look.growthPerLife },
            uDragPerSecond: { value: Math.max(look.dragPerSecond, MIN_DRAG_PER_SECOND) },
            uFloor: { value: look.settlesOnFloor ? PRECIPITATE_REST_ANGSTROM : NO_FLOOR },
            uColorStart: { value: new Color(plan.colorStart) },
            uColorEnd: { value: new Color(plan.colorEnd) },
            uIntensity: { value: plan.intensity },
            uOpacity: { value: plan.opacity },
            uFadeInEnd: { value: Math.max(look.fadeInLifeFraction, MIN_FADE_FRACTION) },
            uFadeOutStart: { value: fadeOutStart },
            uFadeOutEnd: { value: fadeOutStart + Math.max(look.fadeOutLifeFraction, MIN_FADE_FRACTION) },
            uAlphaCutoff: { value: look.opaque ? OPAQUE_ALPHA_CUTOFF : 0 }
        }
    });
}

function blendingOf(look: ParticleLook): Blending {
    if (look.opaque) {
        return NoBlending;
    }

    return look.additive ? AdditiveBlending : NormalBlending;
}

function vary(variation: Variation, fraction: number): number {
    return variation.minimum + variation.range * fraction;
}

function randomDirection(azimuthFraction: number, elevationFraction: number, target: Vector3): Vector3 {
    const azimuth = azimuthFraction * Math.PI * 2;
    const polar = Math.acos(2 * elevationFraction - 1);

    return target.set(Math.sin(polar) * Math.cos(azimuth), Math.cos(polar), Math.sin(polar) * Math.sin(azimuth));
}