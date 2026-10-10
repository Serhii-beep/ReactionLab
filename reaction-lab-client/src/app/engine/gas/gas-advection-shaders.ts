import { VOXEL_OR_EXIT } from "./gas-grid";

const BACKTRACE = `
    vec3 backtrace(sampler2D velocityField, vec3 p, float dt) {
        vec4 start = fetchVoxel(velocityField, ivec3(floor(p)));
        vec3 middle = p - 0.5 * dt * start.xyz;
        vec4 halfway = sampleGrid(velocityField, middle);

        return p - dt * halfway.xyz;
    }
`;

export const ADVECT_VELOCITY_SHADER = `
    uniform sampler2D uVelocity;
    uniform float uDt;
    uniform float uVelocityKeep;

    void main() {
        ${VOXEL_OR_EXIT}
        vec3 velocity = fetchVoxel(uVelocity, voxel).xyz;
        vec3 middle = p - 0.5 * uDt * velocity;
        vec3 back = p - uDt * sampleGrid(uVelocity, middle).xyz;

        fragColor = vec4(sampleGrid(uVelocity, back).xyz * pow(uVelocityKeep, uDt), 0.0);
    }
`;

export const ESTIMATE_SCALARS_SHADER = `
    uniform sampler2D uVelocity;
    uniform sampler2D uScalars;
    uniform float uDt;
    ${BACKTRACE}

    void main() {
        ${VOXEL_OR_EXIT}
        vec3 back = backtrace(uVelocity, p, uDt);

        fragColor = sampleGrid(uScalars, back);
    }
`;

export const ADVECT_SCALARS_SHADER = `
    const float EDGE_SHARE = 0.35;
    const float EDGE_KEEP_PER_SECOND = 0.02;

    uniform sampler2D uVelocity;
    uniform sampler2D uScalars;
    uniform sampler2D uEstimate;
    uniform float uDt;
    uniform vec4 uScalarKeep;
    uniform vec4 uSourcePlaces[MAX_GAS_SOURCES];
    uniform vec4 uSourceFeeds[MAX_GAS_SOURCES];
    uniform float uSourceClouds[MAX_GAS_SOURCES];
    uniform int uSourceCount;
    ${BACKTRACE}

    vec4 corrected(vec3 p, ivec3 voxel) {
        vec3 back = backtrace(uVelocity, p, uDt);
        vec4 start = fetchVoxel(uVelocity, voxel);
        vec4 halfway = sampleGrid(uVelocity, p + 0.5 * uDt * start.xyz);
        vec3 ahead = p + uDt * halfway.xyz;
        vec4 estimate = fetchVoxel(uEstimate, voxel);
        vec4 current = fetchVoxel(uScalars, voxel);
        vec4 estimateAhead = sampleGrid(uEstimate, ahead);
        vec4 value = estimate + 0.5 * (current - estimateAhead);
        ivec3 cell = ivec3(floor(back - 0.5));
        vec4 lowest = fetchVoxel(uScalars, cell);
        vec4 highest = lowest;

        for (int corner = 1; corner < 8; corner++) {
            vec4 neighbor = fetchVoxel(uScalars, cell + ivec3(corner & 1, (corner >> 1) & 1, corner >> 2));

            lowest = min(lowest, neighbor);
            highest = max(highest, neighbor);
        }

        return clamp(value, lowest, highest);
    }

    void main() {
        ${VOXEL_OR_EXIT}
        vec4 value = corrected(p, voxel) * pow(uScalarKeep, vec4(uDt));
        value *= pow(mix(EDGE_KEEP_PER_SECOND, 1.0, smoothstep(0.0, EDGE_SHARE * uVoxelsPerSide, distanceToWalls(p))), uDt);

        for (int index = 0; index < MAX_GAS_SOURCES; index++) {
            if (index >= uSourceCount) {
                break;
            }

            vec3 offset = p - uSourcePlaces[index].xyz;
            float radius = uSourcePlaces[index].w;
            float weight = exp(-dot(offset, offset) / (radius * radius));
            vec4 feed = uSourceFeeds[index];

            value.r += uDt * weight * feed.y * max(feed.x - value.r, 0.0);
            value.gb += uDt * weight * feed.zw;
            value.a += uDt * weight * uSourceClouds[index];
        }

        fragColor = max(value, vec4(0.0));
    }
`;

export const ADVECT_COORDINATES_SHADER = `
    uniform sampler2D uVelocity;
    uniform sampler2D uCoordinates;
    uniform float uDt;
    uniform float uReset;
    uniform float uVoxelSize;
    ${BACKTRACE}

    void main() {
        ${VOXEL_OR_EXIT}
        if (uReset > 0.5) {
            fragColor = vec4(0.0, 0.0, 0.0, 1.0);

            return;
        }

        vec3 back = backtrace(uVelocity, p, uDt);
        vec4 displaced = sampleGrid(uCoordinates, back);

        fragColor = vec4(displaced.xyz + (back - p) * uVoxelSize, 1.0);
    }
`;
