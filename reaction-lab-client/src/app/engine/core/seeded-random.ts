export type RandomSource = () => number;

const FNV_OFFSET_BASIS = 2166136261;
const FNV_PRIME = 16777619;
const MULBERRY_INCREMENT = 0x6d2b79f5;
const UINT32_RANGE = 4294967296;
const SMALLEST_UNIFORM = 1e-12;

export function seededRandom(seed: string): RandomSource {
    let state = hashOf(seed);

    return () => {
        state = (state + MULBERRY_INCREMENT) | 0;

        let mixed = Math.imul(state ^ (state >>> 15), 1 | state);

        mixed = (mixed + Math.imul(mixed ^ (mixed >>> 7), 61 | mixed)) ^ mixed;

        return ((mixed ^ (mixed >>> 14)) >>> 0) / UINT32_RANGE;
    };
}

export function standardNormalSource(uniform: RandomSource): RandomSource {
    let spare: number | null = null;

    return () => {
        if (spare !== null) {
            const deviate = spare;

            spare = null;

            return deviate;
        }

        const radius = Math.sqrt(-2 * Math.log(Math.max(uniform(), SMALLEST_UNIFORM)));
        const angle = 2 * Math.PI * uniform();

        spare = radius * Math.sin(angle);

        return radius * Math.cos(angle);
    };
}

function hashOf(text: string): number {
    let hash = FNV_OFFSET_BASIS;

    for (let index = 0; index < text.length; index++) {
        hash = Math.imul(hash ^ text.charCodeAt(index), FNV_PRIME);
    }

    return hash >>> 0;
}