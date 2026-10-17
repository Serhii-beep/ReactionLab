import { ReactionConditions, ReactionEquilibrium } from "../../../core/chemistry/chemistry-contracts";
import { ActivationSource } from "../../../engine/animation/reaction-script";
import { KELVIN_AT_ZERO_CELSIUS, STANDARD_KELVIN } from "../../../state/lab-conditions-store";
import { waterBoilingKelvinAt } from "./water-boiling";

export type EquilibriumLean = 'complete' | 'equilibrium' | 'reactants';

export type UphillDrive = Exclude<ActivationSource, 'heat'>;

export type ConditionsFixKind = 'heat' | 'cool' | 'standard';

export interface ConditionsFix {
    readonly kind: ConditionsFixKind;
    readonly kelvin: number;
}

export interface ConditionsVerdict {
    readonly lean: EquilibriumLean | null;
    readonly conversion: number | null;
    readonly log10RateFactor: number | null;
    readonly boils: boolean;
    readonly drivenUphillBy: UphillDrive | null;
    readonly blocksRun: boolean;
    readonly fix: ConditionsFix | null;
}

export interface VerdictSetting {
    readonly kelvin: number;
    readonly kilopascals: number;
    readonly inWater: boolean;
    readonly activationSource: ActivationSource;
}

const COMPLETE_CONVERSION = 0.99;
const BARELY_CONVERSION = 0.01;
const SUGGESTION_STEP_CELSIUS = 25;

export function conditionsVerdictOf(conditions: ReactionConditions, setting: VerdictSetting): ConditionsVerdict {
    const lean = leanOf(conditions.equilibrium);
    const boils = setting.inWater && setting.kelvin >= waterBoilingKelvinAt(setting.kilopascals);
    const drivenUphillBy = lean === 'reactants' ? uphillDriveOf(setting.activationSource) : null;
    const thermallyBlocked = lean === 'reactants' && drivenUphillBy === null;

    return {
        lean,
        conversion: conditions.equilibrium?.conversion ?? null,
        log10RateFactor: conditions.kinetics?.log10RateFactor ?? null,
        boils,
        drivenUphillBy,
        blocksRun: thermallyBlocked || boils,
        fix: fixOf(boils, thermallyBlocked ? conditions.equilibrium : undefined, setting.kelvin)
    };
}

function leanOf(equilibrium: ReactionEquilibrium | undefined): EquilibriumLean | null {
    if (equilibrium === undefined) {
        return null;
    }

    if (equilibrium.conversion >= COMPLETE_CONVERSION) {
        return 'complete';
    }

    return equilibrium.conversion <= BARELY_CONVERSION ? 'reactants' : 'equilibrium';
}

function uphillDriveOf(source: ActivationSource): UphillDrive | null {
    return source === 'heat' ? null : source;
}

function fixOf(boils: boolean, blockedEquilibrium: ReactionEquilibrium | undefined, kelvin: number): ConditionsFix | null {
    const halfConversionKelvin = blockedEquilibrium?.halfConversionKelvin;

    if (boils) {
        return { kind: 'standard', kelvin: STANDARD_KELVIN };
    }

    return halfConversionKelvin === undefined ? null : temperatureFixOf(halfConversionKelvin, kelvin);
}

function temperatureFixOf(halfConversionKelvin: number, kelvin: number): ConditionsFix {
    const heats = halfConversionKelvin > kelvin;
    const steps = (halfConversionKelvin - KELVIN_AT_ZERO_CELSIUS) / SUGGESTION_STEP_CELSIUS;

    return {
        kind: heats ? 'heat' : 'cool',
        kelvin: (heats ? Math.ceil(steps) : Math.floor(steps)) * SUGGESTION_STEP_CELSIUS + KELVIN_AT_ZERO_CELSIUS
    };
}
