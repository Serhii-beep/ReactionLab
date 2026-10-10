import { Color, MeshPhysicalMaterial, WebGLProgramParametersWithUniforms } from "three";
import { Disposable } from "../core/disposal-scope";
import { Phase } from "../core/matter";
import { ReactionLight } from "../rendering/reaction-light";

interface Finish {
    roughness: number;
    clearcoat: number;
    emissive: number;
}

const FINISHES: Readonly<Record<Phase, Finish>> = {
    solid: { roughness: 0.35, clearcoat: 0.15, emissive: 0 },
    liquid: { roughness: 0.2, clearcoat: 0.4, emissive: 0 },
    gas: { roughness: 0.3, clearcoat: 0, emissive: 0 },
    aqueous: { roughness: 0.25, clearcoat: 0.3, emissive: 0 },
    plasma: { roughness: 0.5, clearcoat: 0, emissive: 0.6 },
};
const INCANDESCENT_PROGRAM_KEY = 'atom-incandescence';
const TINT_CHUNK = '#include <color_fragment>';
const EMISSION_CHUNK = '#include <emissivemap_fragment>';

export class MaterialCache implements Disposable {
    private readonly materials = new Map<string, MeshPhysicalMaterial>();

    constructor(private readonly reactionLight: ReactionLight) {}

    get size(): number {
        return this.materials.size;
    }

    atom(symbol: string, phase: Phase, color: Color): MeshPhysicalMaterial {
        return this.get(`${symbol}|${phase}`, () => this.create(color, FINISHES[phase]));
    }

    dispose(): void {
        for (const material of this.materials.values()) {
            material.dispose();
        }

        this.materials.clear();
    }

    private create(color: Color, finish: Finish): MeshPhysicalMaterial {
        const material = new MeshPhysicalMaterial({
            color,
            metalness: 0,
            roughness: finish.roughness,
            clearcoat: finish.clearcoat
        });

        material.emissive.copy(color).multiplyScalar(finish.emissive);
        material.onBeforeCompile = (parameters) => {
            emitInstanceColor(parameters);
            this.reactionLight.patch(parameters);
        };
        material.customProgramCacheKey = () => INCANDESCENT_PROGRAM_KEY;

        return material;
    }

    private get(key: string, create: () => MeshPhysicalMaterial): MeshPhysicalMaterial {
        let material = this.materials.get(key);

        if (!material) {
            material = create();
            this.materials.set(key, material);
        }

        return material;
    }
}

function emitInstanceColor(parameters: WebGLProgramParametersWithUniforms): void {
    if (!parameters.fragmentShader.includes(TINT_CHUNK) || !parameters.fragmentShader.includes(EMISSION_CHUNK)) {
        throw new Error('The atom shader no longer has the chunks that turn the instance color into emitted light.');
    }

    parameters.fragmentShader = parameters.fragmentShader
        .replace(TINT_CHUNK, '')
        .replace(EMISSION_CHUNK, `${EMISSION_CHUNK}\n#ifdef USE_COLOR\n\ttotalEmissiveRadiance += vColor.rgb;\n#endif`);
}