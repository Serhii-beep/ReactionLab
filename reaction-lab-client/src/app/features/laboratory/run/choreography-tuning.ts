import { ReactionSummary } from "../../../data/reactions/reaction";
import { activationBarrierOf } from "../../../data/reactions/reaction-phases";
import { ChoreographyTuning } from "../../../engine/animation/reaction-script";

const TUMBLE_RADIANS_PER_SECOND = 0.9;
const RECOIL_ANGSTROM = { base: 0.12, perBarrier: 0.25 };
const BULGE_ANGSTROM = { base: 0.15, perBarrier: 0.35 };
const JITTER_ANGSTROM = { base: 0.03, perBarrier: 0.07 };
const JITTER_HERTZ = 6;
const RELEASE_EXPONENT = { unknown: 3, endothermic: 1.5, exothermicBase: 2, exothermicPerRelease: 3 };
const RELEASE_REFERENCE_KILOJOULES_PER_MOLE = 600;

export function choreographyTuningFor(reaction: ReactionSummary): ChoreographyTuning {
    const barrier = activationBarrierOf(reaction);

    return {
        tumbleRadiansPerSecond: TUMBLE_RADIANS_PER_SECOND,
        recoilAngstrom: RECOIL_ANGSTROM.base + RECOIL_ANGSTROM.perBarrier * barrier,
        bulgeAngstrom: BULGE_ANGSTROM.base + BULGE_ANGSTROM.perBarrier * barrier,
        jitterAngstrom: JITTER_ANGSTROM.base + JITTER_ANGSTROM.perBarrier * barrier,
        jitterHertz: JITTER_HERTZ,
        releaseExponent: releaseExponentOf(reaction)
    };
}

function releaseExponentOf(reaction: ReactionSummary): number {
    const enthalpy = reaction.enthalpyKilojoulesPerMole;

    if (enthalpy === null) {
        return exponentWithoutEnthalpy(reaction.isExothermic);
    }

    if (enthalpy > 0) {
        return RELEASE_EXPONENT.endothermic;
    }

    const release = Math.min(-enthalpy / RELEASE_REFERENCE_KILOJOULES_PER_MOLE, 1);

    return RELEASE_EXPONENT.exothermicBase + RELEASE_EXPONENT.exothermicPerRelease * release;
}

function exponentWithoutEnthalpy(isExothermic: boolean | null): number {
    if (isExothermic === null) {
        return RELEASE_EXPONENT.unknown;
    }

    return isExothermic ? RELEASE_EXPONENT.exothermicBase : RELEASE_EXPONENT.endothermic;
}