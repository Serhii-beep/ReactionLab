import { Color, IUniform, Vector3, Vector4, WebGLProgramParametersWithUniforms } from "three";
import { PlacedAtom } from "../scene/bench-layout";
import { DRAPER_POINT_KELVIN, incandescenceOf } from "./incandescence";
import { luminanceOf } from "./luminance";

interface ReactionLightUniforms {
    readonly uReactionLightCount: IUniform<number>;
    readonly uReactionLightSpheres: IUniform<Vector4[]>;
    readonly uReactionLightRadiances: IUniform<Color[]>;
}

export interface GlowSource {
    readonly place: Vector3;
    readonly radius: number;
    readonly radiance: Color;
}

interface WeightedGlow extends GlowSource {
    readonly radianceTimesArea: Color;
    readonly weight: number;
}

const MAX_REACTION_LIGHTS = 8;
const COMMON_CHUNK = '#include <common>';
const LIGHTS_END_CHUNK = '#include <lights_fragment_end>';
const SPHERE_LIGHT_DECLARATIONS = `
#define REACTION_LIGHTS ${MAX_REACTION_LIGHTS}
uniform int uReactionLightCount;
uniform vec4 uReactionLightSpheres[REACTION_LIGHTS];
uniform vec3 uReactionLightRadiances[REACTION_LIGHTS];

float sphereIlluminance(float cosTheta, float sinSigmaSquared) {
    if (cosTheta * cosTheta > sinSigmaSquared) {
        return PI * sinSigmaSquared * saturate(cosTheta);
    }

    float sinTheta = max(sqrt(1.0 - cosTheta * cosTheta), 1e-4);
    float cotSigma = sqrt(1.0 / sinSigmaSquared - 1.0);
    float horizonCosine = clamp(-cotSigma * cosTheta / sinTheta, -1.0, 1.0);
    float horizonSine = sinTheta * sqrt(1.0 - horizonCosine * horizonCosine);

    return max((cosTheta * acos(horizonCosine) - cotSigma * horizonSine) * sinSigmaSquared + atan(horizonSine, cotSigma), 0.0);
}
`;
const SPHERE_LIGHT_SHADING = `
for (int index = 0; index < REACTION_LIGHTS; index++) {
    if (index >= uReactionLightCount) {
        break;
    }

    vec4 sphere = uReactionLightSpheres[index];
    vec3 toLight = (viewMatrix * vec4(sphere.xyz, 1.0)).xyz - geometryPosition;
    float distanceSquared = max(dot(toLight, toLight), 1e-6);
    float sinSigmaSquared = min(sphere.w * sphere.w / distanceSquared, 1.0);
    float cosTheta = dot(geometryNormal, toLight * inversesqrt(distanceSquared));

    reflectedLight.directDiffuse += uReactionLightRadiances[index] * sphereIlluminance(cosTheta, sinSigmaSquared) * BRDF_Lambert(material.diffuseColor);
}
`;

export class ReactionLight {
    private readonly uniforms: ReactionLightUniforms = {
        uReactionLightCount: { value: 0 },
        uReactionLightSpheres: { value: Array.from({ length: MAX_REACTION_LIGHTS }, () => new Vector4()) },
        uReactionLightRadiances: { value: Array.from({ length: MAX_REACTION_LIGHTS }, () => new Color()) }
    };
    private atomGlows: readonly WeightedGlow[] = [];
    private gasGlows: readonly WeightedGlow[] = [];

    patch(parameters: WebGLProgramParametersWithUniforms): void {
        if (!parameters.fragmentShader.includes(COMMON_CHUNK) || !parameters.fragmentShader.includes(LIGHTS_END_CHUNK)) {
            throw new Error('The lit shader no longer has the chunks the reaction light shades after.');
        }

        Object.assign(parameters.uniforms, this.uniforms);
        parameters.fragmentShader = parameters.fragmentShader
            .replace(COMMON_CHUNK, `${COMMON_CHUNK}\n${SPHERE_LIGHT_DECLARATIONS}`)
            .replace(LIGHTS_END_CHUNK, `${LIGHTS_END_CHUNK}\n${SPHERE_LIGHT_SHADING}`);
    }

    shine(atoms: readonly PlacedAtom[]): void {
        this.atomGlows = atomGlowsOf(atoms);
        this.gather();
    }

    glowFromGas(sources: readonly GlowSource[]): void {
        this.gasGlows = sources.map(weighted);
        this.gather();
    }

    private gather(): void {
        const glows = [...this.atomGlows, ...this.gasGlows].sort((first, second) => second.weight - first.weight);
        const seeds = glows.slice(0, MAX_REACTION_LIGHTS);
        const membersBySeed = new Map(seeds.map((seed) => [seed, [seed]]));

        for (const glow of glows.slice(MAX_REACTION_LIGHTS)) {
            membersBySeed.get(nearestOf(seeds, glow.place))?.push(glow);
        }

        [...membersBySeed.values()].forEach((members, index) => this.place(members, index));
        this.uniforms.uReactionLightCount.value = membersBySeed.size;
    }

    private place(members: readonly WeightedGlow[], index: number): void {
        const totalWeight = members.reduce((sum, member) => sum + member.weight, 0);
        const center = members.reduce((sum, member) => sum.addScaledVector(member.place, member.weight / totalWeight), new Vector3());
        const spreadSquared = members.reduce((sum, member) => sum + (member.weight / totalWeight) * (member.place.distanceToSquared(center) + member.radius ** 2), 0);
        const radiance = this.uniforms.uReactionLightRadiances.value[index].setRGB(0, 0, 0);

        for (const member of members) {
            radiance.add(member.radianceTimesArea);
        }

        radiance.multiplyScalar(1 / spreadSquared);
        this.uniforms.uReactionLightSpheres.value[index].set(center.x, center.y, center.z, Math.sqrt(spreadSquared));
    }
}

function atomGlowsOf(atoms: readonly PlacedAtom[]): WeightedGlow[] {
    return atoms
        .filter((atom) => atom.temperatureKelvin > DRAPER_POINT_KELVIN)
        .map((atom) => weighted({ place: atom.position, radius: atom.radius, radiance: incandescenceOf(atom.temperatureKelvin, new Color()) }))
        .filter((glow) => glow.weight > 0);
}

function weighted(source: GlowSource): WeightedGlow {
    const radianceTimesArea = source.radiance.clone().multiplyScalar(source.radius ** 2);

    return { ...source, radianceTimesArea, weight: luminanceOf(radianceTimesArea) };
}

function nearestOf(seeds: readonly WeightedGlow[], place: Vector3): WeightedGlow {
    return seeds.reduce((nearest, seed) => seed.place.distanceToSquared(place) < nearest.place.distanceToSquared(place) ? seed : nearest);
}
