import { ROOM_TEMPERATURE_KELVIN } from "../core/matter";
import { INCANDESCENCE_SHADER } from "../rendering/incandescence-texture";

export const GLOW_CELL_VOXELS = 4;
export const GLOW_GROUP_CELLS = 4;
export const GLOW_BLOCKS_PER_SIDE = 4;

const MAX_SPAN = 4;

const GRID_LAYOUT = `
    uniform vec2 uDestination;

    ivec2 gridTexel(ivec3 cell, int side, int tilesX) {
        return ivec2((cell.z % tilesX) * side + cell.x, (cell.z / tilesX) * side + cell.y);
    }

    ivec3 destinationCell() {
        int side = int(uDestination.x);
        ivec2 texel = ivec2(gl_FragCoord.xy);
        ivec2 tile = texel / side;

        return ivec3(texel - tile * side, tile.y * int(uDestination.y) + tile.x);
    }
`;

export const GLOW_CELLS_SHADER = `
    ${INCANDESCENCE_SHADER}
    ${GRID_LAYOUT}

    uniform sampler2D uScalars;
    uniform float uMistExtinction;
    uniform float uParticleAbsorption;
    uniform float uVoxelSize;
    uniform vec3 uFlameLight;

    const int CELL_VOXELS = ${GLOW_CELL_VOXELS};
    const float ROOM_KELVIN = ${ROOM_TEMPERATURE_KELVIN.toFixed(1)};
    const float FLAME_BODY = 0.12;

    void main() {
        ivec3 cell = destinationCell();
        vec3 emission = vec3(0.0);
        float absorption = 0.0;

        if (cell.z < int(uDestination.x)) {
            for (int layer = 0; layer < CELL_VOXELS; layer++) {
                for (int row = 0; row < CELL_VOXELS; row++) {
                    for (int column = 0; column < CELL_VOXELS; column++) {
                        vec4 scalars = fetchVoxel(uScalars, cell * CELL_VOXELS + ivec3(column, row, layer));
                        vec3 glow = incandescence(ROOM_KELVIN + scalars.r);

                        emission += uFlameLight * FLAME_BODY * (1.0 - exp(-scalars.b)) + scalars.a * uMistExtinction * glow;
                        absorption += scalars.a * uMistExtinction * uParticleAbsorption;
                    }
                }
            }
        }

        fragColor = vec4(emission, absorption) * (uVoxelSize * uVoxelSize * uVoxelSize);
    }
`;

export const GLOW_REDUCE_SHADER = `
    ${GRID_LAYOUT}

    uniform sampler2D uSource;
    uniform vec2 uSourceGrid;

    const int MAX_SPAN = ${MAX_SPAN};

    void main() {
        ivec3 cell = destinationCell();
        int sourceSide = int(uSourceGrid.x);
        int span = sourceSide / int(uDestination.x);
        vec4 total = vec4(0.0);

        if (cell.z < int(uDestination.x)) {
            for (int layer = 0; layer < MAX_SPAN; layer++) {
                for (int row = 0; row < MAX_SPAN; row++) {
                    for (int column = 0; column < MAX_SPAN; column++) {
                        if (max(max(column, row), layer) < span) {
                            total += texelFetch(uSource, gridTexel(cell * span + ivec3(column, row, layer), sourceSide, int(uSourceGrid.y)), 0);
                        }
                    }
                }
            }
        }

        fragColor = total;
    }
`;
