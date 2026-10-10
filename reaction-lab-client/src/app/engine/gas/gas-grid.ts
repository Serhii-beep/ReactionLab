export const MAX_GAS_SOURCES = 40;
export const MAX_GAS_OBSTACLES = 48;

export const GAS_PASS_VERTEX_SHADER = `
    precision highp float;
    in vec3 position;

    void main() {
        gl_Position = vec4(position.xy, 0.0, 1.0);
    }
`;

export const GAS_GRID_SHADER = `
    uniform float uVoxelsPerSide;
    uniform vec2 uTiles;
    uniform vec2 uAtlas;

    ivec2 atlasTexel(ivec3 voxel) {
        int side = int(uVoxelsPerSide);
        int tilesX = int(uTiles.x);

        return ivec2((voxel.z % tilesX) * side + voxel.x, (voxel.z / tilesX) * side + voxel.y);
    }

    ivec3 voxelAt(ivec2 texel) {
        int side = int(uVoxelsPerSide);
        ivec2 tile = texel / side;

        return ivec3(texel - tile * side, tile.y * int(uTiles.x) + tile.x);
    }

    vec4 fetchVoxel(sampler2D field, ivec3 voxel) {
        int last = int(uVoxelsPerSide) - 1;

        return texelFetch(field, atlasTexel(clamp(voxel, ivec3(0), ivec3(last))), 0);
    }

    vec2 tileOrigin(float slice) {
        float row = floor((slice + 0.5) / uTiles.x);

        return vec2(slice - row * uTiles.x, row) * uVoxelsPerSide;
    }

    float distanceToWalls(vec3 p) {
        return min(uVoxelsPerSide - p.y, 0.5 * uVoxelsPerSide - length(p.xz - 0.5 * uVoxelsPerSide));
    }

    vec4 sampleGrid(sampler2D field, vec3 p) {
        p = clamp(p, vec3(0.5), vec3(uVoxelsPerSide - 0.5));

        float slice = p.z - 0.5;
        float lower = floor(slice);
        float upper = min(lower + 1.0, uVoxelsPerSide - 1.0);
        vec4 below = texture(field, (tileOrigin(lower) + p.xy) / uAtlas);
        vec4 above = texture(field, (tileOrigin(upper) + p.xy) / uAtlas);

        return mix(below, above, slice - lower);
    }
`;

export const GAS_PASS_HEADER = `
    precision highp float;
    precision highp int;
    precision highp sampler2D;
    #define MAX_GAS_SOURCES ${MAX_GAS_SOURCES}
    #define MAX_GAS_OBSTACLES ${MAX_GAS_OBSTACLES}
    ${GAS_GRID_SHADER}
    out vec4 fragColor;
`;

export const VOXEL_OR_EXIT = `
    ivec3 voxel = voxelAt(ivec2(gl_FragCoord.xy));

    if (voxel.z >= int(uVoxelsPerSide)) {
        fragColor = vec4(0.0);
        return;
    }

    vec3 p = vec3(voxel) + 0.5;
`;