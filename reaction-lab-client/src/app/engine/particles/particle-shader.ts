export const PARTICLE_VERTEX_SHADER = `
    attribute vec3 origin;
    attribute vec3 velocity;
    attribute vec4 lifecycle;

    uniform float uElapsedSeconds;
    uniform vec3 uGravity;
    uniform float uGrowthPerLife;
    uniform float uDragPerSecond;
    uniform float uFloor;

    varying vec2 vCorner;
    varying float vAge;
    varying float vSeed;

    void main() {
        float birth = lifecycle.x;
        float life = lifecycle.y;
        float size = lifecycle.z;
        float sinceBirthSigned = uElapsedSeconds - birth;
        float sinceBirth = max(sinceBirthSigned, 0.0);
        float age = sinceBirth / life;
        float alive = step(0.0, sinceBirthSigned) * (1.0 - step(1.0, age));
        float travelled = (1.0 - exp(-uDragPerSecond * sinceBirth)) / uDragPerSecond;
        vec3 center = origin + velocity * travelled + 0.5 * uGravity * sinceBirth * sinceBirth;
        float extent = size * (1.0 + uGrowthPerLife * age) * alive;

        center.y = max(center.y, uFloor + 0.5 * extent);
        vCorner = position.xy;
        vAge = age;
        vSeed = lifecycle.w;

        vec4 mvPosition = modelViewMatrix * vec4(center, 1.0);
        mvPosition.xy += position.xy * extent;
        gl_Position = projectionMatrix * mvPosition;
    }
`;

export const PARTICLE_FRAGMENT_SHADER = `
    uniform vec3 uColorStart;
    uniform vec3 uColorEnd;
    uniform float uIntensity;
    uniform float uOpacity;
    uniform float uFadeInEnd;
    uniform float uFadeOutStart;
    uniform float uFadeOutEnd;
    uniform float uAlphaCutoff;

    varying vec2 vCorner;
    varying float vAge;
    varying float vSeed;

    const float CORNER_TO_RADIUS = 2.0;
    const float SOFT_CORE_MIN = 0.2;
    const float SOFT_CORE_RANGE = 0.3;

    void main() {
        float radial = length(vCorner) * CORNER_TO_RADIUS;
        float coreEdge = SOFT_CORE_MIN + SOFT_CORE_RANGE * vSeed;
        float softness = 1.0 - smoothstep(coreEdge, 1.0, radial);
        float envelope = smoothstep(0.0, uFadeInEnd, vAge) * (1.0 - smoothstep(uFadeOutStart, uFadeOutEnd, vAge));
        float alpha = softness * envelope * uOpacity;

        if (alpha < uAlphaCutoff) {
            discard;
        }

        gl_FragColor = vec4(mix(uColorStart, uColorEnd, vAge) * uIntensity, alpha);

        #include <tonemapping_fragment>
        #include <colorspace_fragment>
    }
`;