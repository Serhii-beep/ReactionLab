import { GAS_GRID_SHADER } from "../gas/gas-grid";
import { INCANDESCENCE_SHADER } from "../rendering/incandescence-texture";
import { LIFE_PER_COOLING, SPARK_SIDE } from "./spark-launch";

export const SPARK_STEP_SHADER = `
    precision highp float;
    precision highp int;
    precision highp sampler2D;
    ${GAS_GRID_SHADER}

    uniform sampler2D uPlaces;
    uniform sampler2D uMotions;
    uniform sampler2D uLaunchPlaces;
    uniform sampler2D uLaunchMotions;
    uniform sampler2D uLaunchTraits;
    uniform sampler2D uAirVelocity;
    uniform sampler2D uAirScalars;
    uniform float uAirflow;
    uniform vec3 uOrigin;
    uniform float uVoxelSize;
    uniform float uSeconds;
    uniform float uDt;
    uniform float uRoomKelvin;

    layout(location = 0) out vec4 outPlace;
    layout(location = 1) out vec4 outMotion;

    const float LIFE_PER_COOLING = ${LIFE_PER_COOLING.toFixed(1)};
    const float FLOOR_REST = 0.015;
    const float BOUNCE_KEEP = 0.3;
    const float SLIDE_KEEP = 0.55;

    vec4 airAt(vec3 place) {
        vec3 voxel = (place - uOrigin) / uVoxelSize;

        if (uAirflow < 0.5 || any(lessThan(voxel, vec3(0.0))) || any(greaterThan(voxel, vec3(uVoxelsPerSide)))) {
            return vec4(0.0, 0.0, 0.0, uRoomKelvin);
        }

        vec4 air = sampleGrid(uAirVelocity, voxel);
        vec4 scalars = sampleGrid(uAirScalars, voxel);

        return vec4(air.xyz * uVoxelSize, uRoomKelvin + scalars.r);
    }

    void main() {
        ivec2 texel = ivec2(gl_FragCoord.xy);
        vec4 launchPlace = texelFetch(uLaunchPlaces, texel, 0);
        vec4 launchMotion = texelFetch(uLaunchMotions, texel, 0);
        vec4 traits = texelFetch(uLaunchTraits, texel, 0);
        float age = uSeconds - launchPlace.w;

        if (age < 0.0 || age > traits.z * LIFE_PER_COOLING) {
            outPlace = vec4(launchPlace.xyz, -1.0);
            outMotion = vec4(launchMotion.xyz, 0.0);

            return;
        }

        vec4 place = texelFetch(uPlaces, texel, 0);
        vec4 motion = texelFetch(uMotions, texel, 0);
        float dt = uDt;

        if (place.w < 0.0) {
            place = vec4(launchPlace.xyz, 0.0);
            motion = launchMotion;
            dt = min(age, uDt);
        }

        vec4 air = airAt(place.xyz);

        motion.xyz += (air.xyz - motion.xyz) * (1.0 - exp(-traits.x * dt));
        motion.y += traits.y * dt;
        motion.w = air.w + (motion.w - air.w) * exp(-dt / traits.z);
        place.xyz += motion.xyz * dt;

        if (place.y < FLOOR_REST) {
            place.y = FLOOR_REST;
            motion.y = abs(motion.y) * BOUNCE_KEEP;
            motion.xz *= SLIDE_KEEP;
        }

        outPlace = vec4(place.xyz, age);
        outMotion = motion;
    }
`;

export const SPARK_STREAK_VERTEX_SHADER = `
    uniform sampler2D uPlaces;
    uniform sampler2D uMotions;
    uniform sampler2D uLaunchTraits;
    uniform vec2 uResolution;
    uniform float uPixelRatio;
    uniform float uShutterSeconds;
    uniform float uBrightness;

    varying vec3 vRadiance;
    varying float vAcross;
    varying float vAlong;

    const int SPARK_SIDE = ${SPARK_SIDE};
    const float LIFE_PER_COOLING = ${LIFE_PER_COOLING.toFixed(1)};
    const float NEAREST_CLIP_W = 0.05;
    const float FADE_IN_SECONDS = 0.03;
    const float FADE_OUT_LIFE_SHARE = 0.75;
    const float BLUR_SPREAD_SHARE = 0.55;
    ${INCANDESCENCE_SHADER}

    void main() {
        ivec2 texel = ivec2(gl_InstanceID % SPARK_SIDE, gl_InstanceID / SPARK_SIDE);
        vec4 place = texelFetch(uPlaces, texel, 0);
        vec4 motion = texelFetch(uMotions, texel, 0);
        vec4 clipHead = projectionMatrix * viewMatrix * vec4(place.xyz, 1.0);
        vec4 clipTail = projectionMatrix * viewMatrix * vec4(place.xyz - motion.xyz * uShutterSeconds, 1.0);

        vRadiance = vec3(0.0);
        vAcross = 0.0;
        vAlong = 0.0;

        if (place.w < 0.0 || motion.w <= uIncandescenceKelvin.x || min(clipHead.w, clipTail.w) < NEAREST_CLIP_W) {
            gl_Position = vec4(0.0, 0.0, 2.0, 1.0);

            return;
        }

        vec4 traits = texelFetch(uLaunchTraits, texel, 0);
        vec2 head = clipHead.xy / clipHead.w * 0.5 * uResolution;
        vec2 tail = clipTail.xy / clipTail.w * 0.5 * uResolution;
        float streak = length(head - tail);
        vec2 along = streak > 1e-3 ? (head - tail) / streak : vec2(0.0, 1.0);
        vec2 across = vec2(-along.y, along.x);
        float width = traits.w * uPixelRatio;
        vec2 screen = mix(tail - along * width * 0.5, head + along * width * 0.5, position.y) + across * position.x * width;
        float clipW = mix(clipTail.w, clipHead.w, position.y);
        float life = traits.z * LIFE_PER_COOLING;
        float fade = smoothstep(0.0, FADE_IN_SECONDS, place.w) * (1.0 - smoothstep(FADE_OUT_LIFE_SHARE * life, life, place.w));

        gl_Position = vec4(screen / (0.5 * uResolution) * clipW, mix(clipTail.z, clipHead.z, position.y), clipW);
        vRadiance = incandescence(motion.w) * uBrightness * fade * mix(1.0, width / (streak + width), BLUR_SPREAD_SHARE);
        vAcross = position.x * 2.0;
        vAlong = position.y;
    }
`;

export const SPARK_STREAK_FRAGMENT_SHADER = `
    varying vec3 vRadiance;
    varying float vAcross;
    varying float vAlong;

    const float TAIL_SHARE = 0.25;
    const float HEAD_START = 0.6;

    void main() {
        float core = 1.0 - vAcross * vAcross;

        gl_FragColor = vec4(vRadiance * core * core * mix(TAIL_SHARE, 1.0, smoothstep(0.0, HEAD_START, vAlong)), 1.0);

        #include <tonemapping_fragment>
        #include <colorspace_fragment>
    }
`;
