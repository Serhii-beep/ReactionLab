import { Color, DataTexture, FloatType, NearestFilter, RGBAFormat } from "three";
import { FLAME_CEILING_KELVIN } from "../core/matter";
import { DRAPER_POINT_KELVIN, incandescenceOf } from "../rendering/incandescence";

const ENTRIES = 256;
const CHANNELS = 4;

export function incandescenceTexture(): DataTexture {
    const data = new Float32Array(ENTRIES * CHANNELS);
    const color = new Color();

    for (let entry = 0; entry < ENTRIES; entry++) {
        const kelvin = DRAPER_POINT_KELVIN + ((FLAME_CEILING_KELVIN - DRAPER_POINT_KELVIN) * entry) / (ENTRIES - 1);

        incandescenceOf(kelvin, color).toArray(data, entry * CHANNELS);
        data[entry * CHANNELS + 3] = 1;
    }

    const texture = new DataTexture(data, ENTRIES, 1, RGBAFormat, FloatType);

    texture.minFilter = NearestFilter;
    texture.magFilter = NearestFilter;
    texture.needsUpdate = true;

    return texture;
}
