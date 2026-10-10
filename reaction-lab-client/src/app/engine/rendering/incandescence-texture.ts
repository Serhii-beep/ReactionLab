import { Color, DataTexture, FloatType, IUniform, NearestFilter, RGBAFormat, Texture, Vector2 } from "three";
import { FLAME_CEILING_KELVIN } from "../core/matter";
import { DRAPER_POINT_KELVIN, incandescenceOf } from "./incandescence";

export interface IncandescenceUniforms {
    readonly uIncandescence: IUniform<Texture>;
    readonly uIncandescenceKelvin: IUniform<Vector2>;
}

export const INCANDESCENCE_SHADER = `
    uniform sampler2D uIncandescence;
    uniform vec2 uIncandescenceKelvin;

    vec3 incandescence(float kelvin) {
        int last = textureSize(uIncandescence, 0).x - 1;
        float entry = clamp((kelvin - uIncandescenceKelvin.x) / (uIncandescenceKelvin.y - uIncandescenceKelvin.x), 0.0, 1.0) * float(last);
        int lower = int(floor(entry));
        vec3 below = texelFetch(uIncandescence, ivec2(lower, 0), 0).rgb;
        vec3 above = texelFetch(uIncandescence, ivec2(min(lower + 1, last), 0), 0).rgb;

        return mix(below, above, entry - float(lower));
    }
`;

const ENTRIES = 256;
const CHANNELS = 4;

export function incandescenceTexture(emissivity = 1): DataTexture {
    const texture = new DataTexture(new Float32Array(ENTRIES * CHANNELS), ENTRIES, 1, RGBAFormat, FloatType);

    texture.minFilter = NearestFilter;
    texture.magFilter = NearestFilter;

    return writeIncandescence(texture, emissivity);
}

export function writeIncandescence(texture: DataTexture, emissivity: number): DataTexture {
    const texels = new Float32Array(ENTRIES * CHANNELS);
    const color = new Color();

    for (let entry = 0; entry < ENTRIES; entry++) {
        const kelvin = DRAPER_POINT_KELVIN + ((FLAME_CEILING_KELVIN - DRAPER_POINT_KELVIN) * entry) / (ENTRIES - 1);

        incandescenceOf(kelvin, color, emissivity).toArray(texels, entry * CHANNELS);
        texels[entry * CHANNELS + 3] = 1;
    }

    texture.image.data = texels;
    texture.needsUpdate = true;

    return texture;
}

export function incandescenceUniformsOf(texture: Texture): IncandescenceUniforms {
    return { uIncandescence: { value: texture }, uIncandescenceKelvin: { value: new Vector2(DRAPER_POINT_KELVIN, FLAME_CEILING_KELVIN) } };
}
