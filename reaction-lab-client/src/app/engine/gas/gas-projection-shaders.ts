import { VOXEL_OR_EXIT } from "./gas-grid";

const OBSTACLES = `
    uniform vec4 uObstaclePlaces[MAX_GAS_OBSTACLES];
    uniform vec3 uObstacleVelocities[MAX_GAS_OBSTACLES];
    uniform int uObstacleCount;

    vec3 obstacleVelocity(vec3 p, vec3 velocity) {
        for (int index = 0; index < MAX_GAS_OBSTACLES; index++) {
            if (index >= uObstacleCount) {
                break;
            }

            vec3 offset = p - uObstaclePlaces[index].xyz;
            float radius = uObstaclePlaces[index].w;
            float inside = 1.0 - smoothstep(radius * 0.8, radius * 1.15, length(offset));

            velocity = mix(velocity, uObstacleVelocities[index], inside);
        }

        return velocity;
    }
`;

const PRESSURE = `
    uniform sampler2D uPressure;

    float pressureAt(ivec3 voxel, float center) {
        int last = int(uVoxelsPerSide) - 1;

        if (voxel.y < 0) {
            return center;
        }

        if (any(lessThan(voxel, ivec3(0))) || any(greaterThan(voxel, ivec3(last)))) {
            return 0.0;
        }

        return texelFetch(uPressure, atlasTexel(voxel), 0).r;
    }
`;

export const CURL_SHADER = `
    uniform sampler2D uVelocity;

    void main() {
        ${VOXEL_OR_EXIT}
        vec3 left = fetchVoxel(uVelocity, voxel - ivec3(1, 0, 0)).xyz;
        vec3 right = fetchVoxel(uVelocity, voxel + ivec3(1, 0, 0)).xyz;
        vec3 down = fetchVoxel(uVelocity, voxel - ivec3(0, 1, 0)).xyz;
        vec3 up = fetchVoxel(uVelocity, voxel + ivec3(0, 1, 0)).xyz;
        vec3 back = fetchVoxel(uVelocity, voxel - ivec3(0, 0, 1)).xyz;
        vec3 front = fetchVoxel(uVelocity, voxel + ivec3(0, 0, 1)).xyz;

        fragColor = vec4(0.5 * vec3((up.z - down.z) - (front.y - back.y), (front.x - back.x) - (right.z - left.z), (right.y - left.y) - (up.x - down.x)), 0.0);
    }
`;

export const FORCES_SHADER = `
    uniform sampler2D uVelocity;
    uniform sampler2D uCurl;
    uniform sampler2D uScalars;
    uniform float uDt;
    uniform float uBuoyancyPerKelvin;
    uniform float uSinkPerCloud;
    uniform float uVorticity;
    uniform float uBurstPerFlame;
    uniform vec4 uSourcePlaces[MAX_GAS_SOURCES];
    uniform vec4 uSourceFeeds[MAX_GAS_SOURCES];
    uniform int uSourceCount;
    ${OBSTACLES}

    vec3 confinement(ivec3 voxel) {
        float left = length(fetchVoxel(uCurl, voxel - ivec3(1, 0, 0)).xyz);
        float right = length(fetchVoxel(uCurl, voxel + ivec3(1, 0, 0)).xyz);
        float down = length(fetchVoxel(uCurl, voxel - ivec3(0, 1, 0)).xyz);
        float up = length(fetchVoxel(uCurl, voxel + ivec3(0, 1, 0)).xyz);
        float back = length(fetchVoxel(uCurl, voxel - ivec3(0, 0, 1)).xyz);
        float front = length(fetchVoxel(uCurl, voxel + ivec3(0, 0, 1)).xyz);
        vec3 gradient = 0.5 * vec3(right - left, up - down, front - back);
        float gradientLength = length(gradient);

        return gradientLength > 1e-5 ? cross(gradient / gradientLength, fetchVoxel(uCurl, voxel).xyz) : vec3(0.0);
    }

    vec3 bursts(vec3 p) {
        vec3 push = vec3(0.0);

        for (int index = 0; index < MAX_GAS_SOURCES; index++) {
            if (index >= uSourceCount) {
                break;
            }

            vec3 offset = p - uSourcePlaces[index].xyz;
            float radius = uSourcePlaces[index].w;
            float distance = length(offset);
            vec3 outward = distance > 1e-4 ? offset / distance : vec3(0.0, 1.0, 0.0);

            push += exp(-distance * distance / (radius * radius)) * uBurstPerFlame * uSourceFeeds[index].w * outward;
        }

        return push;
    }

    void main() {
        ${VOXEL_OR_EXIT}
        vec3 velocity = fetchVoxel(uVelocity, voxel).xyz;
        vec4 scalars = fetchVoxel(uScalars, voxel);

        velocity.y += uDt * (uBuoyancyPerKelvin * scalars.r - uSinkPerCloud * scalars.a);
        velocity += uDt * (uVorticity * confinement(voxel) + bursts(p));
        velocity = obstacleVelocity(p, velocity);

        if (voxel.y == 0) {
            velocity.y = max(velocity.y, 0.0);
        }

        fragColor = vec4(velocity, 0.0);
    }
`;

export const DIVERGENCE_SHADER = `
    uniform sampler2D uVelocity;

    void main() {
        ${VOXEL_OR_EXIT}
        vec3 center = fetchVoxel(uVelocity, voxel).xyz;
        float left = fetchVoxel(uVelocity, voxel - ivec3(1, 0, 0)).x;
        float right = fetchVoxel(uVelocity, voxel + ivec3(1, 0, 0)).x;
        float down = voxel.y == 0 ? -center.y : fetchVoxel(uVelocity, voxel - ivec3(0, 1, 0)).y;
        float up = fetchVoxel(uVelocity, voxel + ivec3(0, 1, 0)).y;
        float back = fetchVoxel(uVelocity, voxel - ivec3(0, 0, 1)).z;
        float front = fetchVoxel(uVelocity, voxel + ivec3(0, 0, 1)).z;

        fragColor = vec4(0.5 * ((right - left) + (up - down) + (front - back)), 0.0, 0.0, 0.0);
    }
`;

export const JACOBI_SHADER = `
    uniform sampler2D uDivergence;
    ${PRESSURE}

    void main() {
        ${VOXEL_OR_EXIT}
        float center = texelFetch(uPressure, atlasTexel(voxel), 0).r;
        float sum = pressureAt(voxel - ivec3(1, 0, 0), center) + pressureAt(voxel + ivec3(1, 0, 0), center)
            + pressureAt(voxel - ivec3(0, 1, 0), center) + pressureAt(voxel + ivec3(0, 1, 0), center)
            + pressureAt(voxel - ivec3(0, 0, 1), center) + pressureAt(voxel + ivec3(0, 0, 1), center);

        fragColor = vec4((sum - texelFetch(uDivergence, atlasTexel(voxel), 0).r) / 6.0, 0.0, 0.0, 0.0);
    }
`;

export const PROJECT_SHADER = `
    uniform sampler2D uVelocity;
    ${PRESSURE}
    ${OBSTACLES}

    void main() {
        ${VOXEL_OR_EXIT}
        float center = texelFetch(uPressure, atlasTexel(voxel), 0).r;
        vec3 gradient = 0.5 * vec3(
            pressureAt(voxel + ivec3(1, 0, 0), center) - pressureAt(voxel - ivec3(1, 0, 0), center),
            pressureAt(voxel + ivec3(0, 1, 0), center) - pressureAt(voxel - ivec3(0, 1, 0), center),
            pressureAt(voxel + ivec3(0, 0, 1), center) - pressureAt(voxel - ivec3(0, 0, 1), center)
        );
        vec3 velocity = obstacleVelocity(p, fetchVoxel(uVelocity, voxel).xyz - gradient);

        if (voxel.y == 0) {
            velocity.y = max(velocity.y, 0.0);
        }

        fragColor = vec4(velocity, 0.0);
    }
`;
