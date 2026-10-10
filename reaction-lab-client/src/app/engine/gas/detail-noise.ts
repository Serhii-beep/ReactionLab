import { BufferAttribute, BufferGeometry, Camera, Data3DTexture, GLSL3, LinearFilter, Mesh, RawShaderMaterial, RepeatWrapping, UnsignedByteType, WebGL3DRenderTarget, WebGLRenderer } from "three";

const NOISE_SIZE = 64;

const NOISE_VERTEX_SHADER = `
    in vec3 position;

    void main() {
        gl_Position = vec4(position.xy, 0.0, 1.0);
    }
`;

const NOISE_FRAGMENT_SHADER = `
    precision highp float;
    precision highp int;

    uniform float uSlice;
    uniform float uSize;

    out vec4 fragColor;

    float hashOf(vec3 cell, float cells, uint seed) {
        uvec3 wrapped = uvec3(mod(cell, cells));
        uint hash = (wrapped.x * 374761393u) ^ (wrapped.y * 668265263u) ^ (wrapped.z * 2147483647u) ^ (seed * 1274126177u);

        hash = (hash ^ (hash >> 13u)) * 1103515245u;
        hash ^= hash >> 16u;

        return float(hash) / 4294967296.0;
    }

    float worley(vec3 p, float cells, uint seed) {
        vec3 scaled = p * cells;
        vec3 base = floor(scaled);
        float nearest = 9.0;

        for (int neighbor = 0; neighbor < 27; neighbor++) {
            vec3 cell = base + vec3(neighbor % 3, (neighbor / 3) % 3, neighbor / 9) - 1.0;
            vec3 feature = cell + vec3(hashOf(cell, cells, seed), hashOf(cell, cells, seed + 1u), hashOf(cell, cells, seed + 2u));

            nearest = min(nearest, distance(feature, scaled));
        }

        return max(0.0, 1.0 - nearest);
    }

    float gradientAt(vec3 corner, vec3 offset, float cells, uint seed) {
        float turn = 6.2831853 * hashOf(corner, cells, seed);
        float height = 2.0 * hashOf(corner, cells, seed + 7u) - 1.0;
        float ring = sqrt(1.0 - height * height);

        return dot(vec3(ring * cos(turn), ring * sin(turn), height), offset);
    }

    float perlin(vec3 p, float cells, uint seed) {
        vec3 scaled = p * cells;
        vec3 base = floor(scaled);
        vec3 fraction = scaled - base;
        vec3 fade = fraction * fraction * fraction * (fraction * (fraction * 6.0 - 15.0) + 10.0);
        float corners[8];

        for (int corner = 0; corner < 8; corner++) {
            vec3 offset = vec3(corner & 1, (corner >> 1) & 1, corner >> 2);

            corners[corner] = gradientAt(base + offset, fraction - offset, cells, seed);
        }

        return 1.4 * mix(
            mix(mix(corners[0], corners[1], fade.x), mix(corners[2], corners[3], fade.x), fade.y),
            mix(mix(corners[4], corners[5], fade.x), mix(corners[6], corners[7], fade.x), fade.y),
            fade.z);
    }

    void main() {
        vec3 p = vec3(gl_FragCoord.xy, uSlice + 0.5) / uSize;
        float fbm = 0.5 * perlin(p, 4.0, 11u) + 0.25 * perlin(p, 8.0, 12u) + 0.125 * perlin(p, 16.0, 13u) + 0.0625 * perlin(p, 32.0, 14u);
        float cellular = 0.625 * worley(p, 4.0, 21u) + 0.25 * worley(p, 8.0, 22u) + 0.125 * worley(p, 16.0, 23u);
        float detail = 0.625 * worley(p, 8.0, 31u) + 0.25 * worley(p, 16.0, 32u) + 0.125 * worley(p, 32.0, 33u);
        float fine = 0.625 * worley(p, 16.0, 41u) + 0.375 * worley(p, 32.0, 42u);

        fragColor = clamp(vec4((fbm * 0.5 + 0.5) * 0.55 + cellular * 0.65 - 0.12, detail, fine, fbm * 0.5 + 0.5), 0.0, 1.0);
    }
`;

export function renderDetailNoise(renderer: WebGLRenderer): WebGL3DRenderTarget {
    const target = new WebGL3DRenderTarget(NOISE_SIZE, NOISE_SIZE, NOISE_SIZE, { type: UnsignedByteType, depthBuffer: false });
    const material = new RawShaderMaterial({
        glslVersion: GLSL3,
        vertexShader: NOISE_VERTEX_SHADER,
        fragmentShader: NOISE_FRAGMENT_SHADER,
        uniforms: { uSlice: { value: 0 }, uSize: { value: NOISE_SIZE } }
    });
    const quad = new Mesh(new BufferGeometry(), material);
    const camera = new Camera();
    const previousTarget = renderer.getRenderTarget();

    quad.geometry.setAttribute('position', new BufferAttribute(new Float32Array([-1, -1, 0, 3, -1, 0, -1, 3, 0]), 3));
    quad.frustumCulled = false;
    repeatSmoothly(target.texture);

    for (let slice = 0; slice < NOISE_SIZE; slice++) {
        material.uniforms['uSlice'].value = slice;
        renderer.setRenderTarget(target, slice);
        renderer.render(quad, camera);
    }

    renderer.setRenderTarget(previousTarget);
    quad.geometry.dispose();
    material.dispose();

    return target;
}

function repeatSmoothly(texture: Data3DTexture): void {
    texture.wrapS = RepeatWrapping;
    texture.wrapT = RepeatWrapping;
    texture.wrapR = RepeatWrapping;
    texture.minFilter = LinearFilter;
    texture.magFilter = LinearFilter;
}
