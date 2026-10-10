import { VOXEL_OR_EXIT } from "./gas-grid";

export const VISIBLE_VAPOR = `
    uniform vec2 uCondensationKelvin;

    float visibleVapor(vec4 scalars) {
        return scalars.g * (1.0 - smoothstep(uCondensationKelvin.y, uCondensationKelvin.x, scalars.r));
    }

    float scatteringOf(vec4 scalars) {
        return visibleVapor(scalars) + scalars.a;
    }
`;

export const GAS_LIGHTING_SHADER = `
    uniform sampler2D uScalars;
    uniform vec3 uLightDirection;
    uniform float uMistExtinction;
    uniform float uVoxelSize;
    ${VISIBLE_VAPOR}

    const int SHADOW_STEPS = 12;
    const float SHADOW_STEP_VOXELS = 2.5;
    const float GLOW_NEAR_VOXELS = 3.0;
    const float GLOW_FAR_VOXELS = 7.0;
    const float GLOW_NEAR_WEIGHT = 0.55;
    const float GLOW_FAR_WEIGHT = 0.3;
    const vec3 GLOW_DIRECTIONS[6] = vec3[6](vec3(1.0, 0.0, 0.0), vec3(-1.0, 0.0, 0.0), vec3(0.0, 1.0, 0.0), vec3(0.0, -1.0, 0.0), vec3(0.0, 0.0, 1.0), vec3(0.0, 0.0, -1.0));

    float flameFront(ivec3 voxel) {
        return length(0.5 * vec3(
            fetchVoxel(uScalars, voxel + ivec3(1, 0, 0)).b - fetchVoxel(uScalars, voxel - ivec3(1, 0, 0)).b,
            fetchVoxel(uScalars, voxel + ivec3(0, 1, 0)).b - fetchVoxel(uScalars, voxel - ivec3(0, 1, 0)).b,
            fetchVoxel(uScalars, voxel + ivec3(0, 0, 1)).b - fetchVoxel(uScalars, voxel - ivec3(0, 0, 1)).b
        ));
    }

    float transmittanceToward(vec3 p, vec3 direction) {
        float depth = 0.0;

        for (int step = 1; step <= SHADOW_STEPS; step++) {
            vec4 shading = sampleGrid(uScalars, p + direction * (float(step) * SHADOW_STEP_VOXELS));

            depth += scatteringOf(shading);
        }

        return exp(-depth * SHADOW_STEP_VOXELS * uVoxelSize * uMistExtinction);
    }

    void main() {
        ${VOXEL_OR_EXIT}
        float glow = fetchVoxel(uScalars, voxel).b;

        for (int direction = 0; direction < 6; direction++) {
            glow += GLOW_NEAR_WEIGHT * sampleGrid(uScalars, p + GLOW_DIRECTIONS[direction] * GLOW_NEAR_VOXELS).b
                + GLOW_FAR_WEIGHT * sampleGrid(uScalars, p + GLOW_DIRECTIONS[direction] * GLOW_FAR_VOXELS).b;
        }

        fragColor = vec4(transmittanceToward(p, uLightDirection), glow / 6.0, flameFront(voxel), transmittanceToward(p, vec3(0.0, 1.0, 0.0)));
    }
`;
