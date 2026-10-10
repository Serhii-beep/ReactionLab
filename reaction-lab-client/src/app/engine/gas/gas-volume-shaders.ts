import { GAS_GRID_SHADER } from "./gas-grid";
import { VISIBLE_VAPOR } from "./gas-lighting-shader";
import { ROOM_TEMPERATURE_KELVIN } from "../core/matter";
import { INCANDESCENCE_SHADER } from "../rendering/incandescence-texture";

export const VOLUME_VERTEX_SHADER = `
    varying vec2 vUv;

    void main() {
        vUv = position.xy * 0.5 + 0.5;
        gl_Position = vec4(position.xy, 1.0, 1.0);
    }
`;

export const VOLUME_MARCH_SHADER = `
    precision highp sampler3D;
    #include <packing>
    ${GAS_GRID_SHADER}
    ${VISIBLE_VAPOR}
    ${INCANDESCENCE_SHADER}

    uniform sampler2D uScalars;
    uniform sampler2D uLighting;
    uniform sampler2D uCoordinatesA;
    uniform sampler2D uCoordinatesB;
    uniform float uFlowWeight;
    uniform sampler3D uNoise;
    uniform sampler2D uDepth;
    uniform mat4 uProjectionInverse;
    uniform mat4 uCameraWorld;
    uniform float uCameraNear;
    uniform float uCameraFar;
    uniform vec3 uOrigin;
    uniform float uVoxelSize;
    uniform float uMistExtinction;
    uniform vec3 uLightDirection;
    uniform vec3 uKeyLight;
    uniform vec3 uSkyLight;
    uniform vec3 uFlameLight;
    uniform vec3 uParticleAlbedo;
    uniform float uFade;

    varying vec2 vUv;

    const float ROOM_KELVIN = ${ROOM_TEMPERATURE_KELVIN.toFixed(1)};
    const int MAX_STEPS = 320;
    const float STEP_VOXELS = 1.5;
    const float EMPTY_STRIDE = 2.0;
    const float GAS_FLOOR = 0.001;
    const float SCATTER_ASYMMETRY = 0.35;
    const float FRONT_SHARPNESS = 0.28;
    const float FRONT_FULL_FLAME = 0.05;
    const float FLAME_BODY = 0.12;
    const float FLAME_GLOW_SHARE = 0.15;
    const float MIST_ONSET = 0.02;
    const float MIST_FULL = 0.2;
    const float MIST_EROSION = 0.4;
    const float COARSE_NOISE_SCALE = 0.3;
    const float FINE_NOISE_SCALE = 0.9;
    const float FINE_NOISE_OFFSET = 0.37;
    const vec3 FIELD_B_OFFSET = vec3(0.53, 0.29, 0.71);

    vec2 boxSpan(vec3 origin, vec3 direction) {
        vec3 inverse = 1.0 / direction;
        vec3 low = (uOrigin - origin) * inverse;
        vec3 high = (uOrigin + vec3(uVoxelsPerSide * uVoxelSize) - origin) * inverse;
        vec3 near = min(low, high);
        vec3 far = max(low, high);

        return vec2(max(max(near.x, near.y), near.z), min(min(far.x, far.y), far.z));
    }

    float sceneDistance(vec3 viewDirection) {
        ivec2 last = textureSize(uDepth, 0) - 1;
        ivec2 corner = clamp(ivec2(vUv * vec2(textureSize(uDepth, 0)) - 0.5), ivec2(0), last);
        float depth = min(
            min(texelFetch(uDepth, corner, 0).r, texelFetch(uDepth, min(corner + ivec2(1, 0), last), 0).r),
            min(texelFetch(uDepth, min(corner + ivec2(0, 1), last), 0).r, texelFetch(uDepth, min(corner + ivec2(1), last), 0).r));

        return depth >= 1.0 ? 1e9 : perspectiveDepthToViewZ(depth, uCameraNear, uCameraFar) / viewDirection.z;
    }

    float scatterPhase(float cosine) {
        float squared = SCATTER_ASYMMETRY * SCATTER_ASYMMETRY;

        return (1.0 - squared) / pow(1.0 + squared - 2.0 * SCATTER_ASYMMETRY * cosine, 1.5);
    }

    float wallFade(vec3 voxel) {
        return smoothstep(1.0, 0.2 * uVoxelsPerSide, distanceToWalls(voxel));
    }

    float flameAt(vec3 carriedA, vec3 carriedB, float flame, float frontGradient) {
        vec4 fine = mix(texture(uNoise, carriedB * FINE_NOISE_SCALE + FINE_NOISE_OFFSET), texture(uNoise, carriedA * FINE_NOISE_SCALE + FINE_NOISE_OFFSET), uFlowWeight);
        float tongue = fine.r * 0.6 + fine.b * 0.4;
        float front = (1.0 - exp(-frontGradient / uVoxelSize * FRONT_SHARPNESS * mix(0.3, 2.2, tongue))) * smoothstep(0.2, 0.7, tongue) * smoothstep(0.0, FRONT_FULL_FLAME, flame);

        return front + FLAME_BODY * (1.0 - exp(-flame * mix(0.25, 1.9, tongue)));
    }

    float mistAt(vec3 carriedA, vec3 carriedB, float vapor) {
        vec4 coarse = mix(texture(uNoise, carriedB * COARSE_NOISE_SCALE), texture(uNoise, carriedA * COARSE_NOISE_SCALE), uFlowWeight);

        return vapor * smoothstep(MIST_ONSET, MIST_FULL, vapor) * mix(1.0 - MIST_EROSION, 1.0 + MIST_EROSION, coarse.g * 0.7 + coarse.r * 0.3);
    }

    void shade(vec3 voxel, vec4 scalars, float fade, float segment, float phase, inout vec4 light) {
        vec3 home = uOrigin + voxel * uVoxelSize;
        vec4 displacedA = sampleGrid(uCoordinatesA, voxel);
        vec4 displacedB = sampleGrid(uCoordinatesB, voxel);
        vec3 carriedA = home + displacedA.xyz;
        vec3 carriedB = home + displacedB.xyz + FIELD_B_OFFSET;
        vec4 lighting = sampleGrid(uLighting, voxel);
        float vapor = visibleVapor(scalars) * fade;
        float particles = scalars.a * fade;
        float scattering = vapor + particles;
        float particleShare = scattering > 0.0 ? particles / scattering : 0.0;
        float glow = scalars.b * fade > GAS_FLOOR ? fade * flameAt(carriedA, carriedB, scalars.b, lighting.b) : 0.0;
        float mist = scattering > MIST_ONSET ? mistAt(carriedA, carriedB, scattering) : 0.0;
        float stepTransmittance = exp(-mist * uMistExtinction * segment);
        vec3 albedo = mix(vec3(1.0), uParticleAlbedo, particleShare);
        vec3 inscatter = (uKeyLight * lighting.r * phase + uSkyLight * lighting.a) * albedo + uFlameLight * lighting.g * FLAME_GLOW_SHARE;
        vec3 particleGlow = particleShare * incandescence(ROOM_KELVIN + scalars.r);

        light.rgb += light.a * (uFlameLight * glow * segment + (inscatter + particleGlow) * (1.0 - stepTransmittance));
        light.a *= stepTransmittance;
    }

    void main() {
        vec4 view = uProjectionInverse * vec4(vUv * 2.0 - 1.0, 1.0, 1.0);
        vec3 viewDirection = normalize(view.xyz / view.w);
        vec3 direction = normalize((uCameraWorld * vec4(viewDirection, 0.0)).xyz);
        vec3 origin = uCameraWorld[3].xyz;
        vec2 span = boxSpan(origin, direction);
        float near = max(span.x, 0.0);
        float far = min(span.y, sceneDistance(viewDirection));
        float stepLength = STEP_VOXELS * uVoxelSize;
        float phase = scatterPhase(dot(direction, uLightDirection));
        float t = near + stepLength * fract(52.9829189 * fract(dot(gl_FragCoord.xy, vec2(0.06711056, 0.00583715))));
        vec4 light = vec4(0.0, 0.0, 0.0, 1.0);

        for (int index = 0; index < MAX_STEPS; index++) {
            if (t >= far || light.a < 0.01) {
                break;
            }

            float segment = min(stepLength, far - t);
            vec3 voxel = (origin + direction * (t + 0.5 * segment) - uOrigin) / uVoxelSize;
            vec4 scalars = sampleGrid(uScalars, voxel);
            float fade = wallFade(voxel);

            if (scalars.b * fade > GAS_FLOOR || scatteringOf(scalars) * fade > MIST_ONSET) {
                shade(voxel, scalars, fade, segment, phase, light);
                t += segment;
            } else {
                t += segment * EMPTY_STRIDE;
            }
        }

        gl_FragColor = vec4(light.rgb, 1.0 - light.a) * uFade;
    }
`;

export const VOLUME_COMPOSITE_SHADER = `
    uniform sampler2D inputBuffer;
    uniform sampler2D uGas;

    varying vec2 vUv;

    void main() {
        vec4 scene = texture(inputBuffer, vUv);
        vec4 gas = texture(uGas, vUv);

        gl_FragColor = vec4(scene.rgb * (1.0 - gas.a) + gas.rgb, scene.a + gas.a * (1.0 - scene.a));
    }
`;
