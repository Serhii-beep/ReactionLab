import { STANDARD_KILOPASCALS } from "../../../state/lab-conditions-store";

const NORMAL_BOILING_KELVIN = 373.15;
const CRITICAL_KELVIN = 647.1;
const VAPORIZATION_JOULES_PER_MOLE = 40650;
const GAS_CONSTANT_JOULES_PER_MOLE_KELVIN = 8.314;

export function waterBoilingKelvinAt(kilopascals: number): number {
    const inverseKelvin = 1 / NORMAL_BOILING_KELVIN
        - (GAS_CONSTANT_JOULES_PER_MOLE_KELVIN * Math.log(kilopascals / STANDARD_KILOPASCALS)) / VAPORIZATION_JOULES_PER_MOLE;

    return Math.min(1 / inverseKelvin, CRITICAL_KELVIN);
}
