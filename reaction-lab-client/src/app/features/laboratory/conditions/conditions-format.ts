import { KELVIN_AT_ZERO_CELSIUS, STANDARD_KILOPASCALS } from "../../../state/lab-conditions-store";

const SUPERSCRIPT_DIGITS: readonly string[] = ['⁰', '¹', '²', '³', '⁴', '⁵', '⁶', '⁷', '⁸', '⁹'];
const SUPERSCRIPT_MINUS = '⁻';
const PLAIN_POWERS = 4;

export const PRESSURE_STOPS_ATMOSPHERES: readonly number[] = [0.01, 0.02, 0.05, 0.1, 0.2, 0.5, 1, 2, 5, 10, 20, 50, 100, 150, 200, 250, 300, 500, 1000];

export function celsiusOf(kelvin: number): number {
    return Math.round(kelvin - KELVIN_AT_ZERO_CELSIUS);
}

export function atmospheresOf(kilopascals: number): number {
    return kilopascals / STANDARD_KILOPASCALS;
}

export function pressureStopOf(kilopascals: number): number {
    const atmospheres = atmospheresOf(kilopascals);
    let nearest = 0;

    PRESSURE_STOPS_ATMOSPHERES.forEach((stop, index) => {
        if (Math.abs(Math.log(stop / atmospheres)) < Math.abs(Math.log(PRESSURE_STOPS_ATMOSPHERES[nearest] / atmospheres))) {
            nearest = index;
        }
    });

    return nearest;
}

export function formatAtmospheres(kilopascals: number, locale: string): string {
    return new Intl.NumberFormat(locale, { maximumSignificantDigits: 3 }).format(atmospheresOf(kilopascals));
}

export function formatPowerOfTen(log10: number, locale: string): string {
    if (Math.abs(log10) < PLAIN_POWERS) {
        return new Intl.NumberFormat(locale, { maximumSignificantDigits: 2 }).format(10 ** log10);
    }

    const exponent = Math.floor(log10);
    const mantissa = Math.round(10 ** (log10 - exponent) * 10) / 10;
    const [shownMantissa, shownExponent] = mantissa >= 10 ? [1, exponent + 1] : [mantissa, exponent];

    return `${new Intl.NumberFormat(locale, { maximumFractionDigits: 1 }).format(shownMantissa)} × 10${superscriptOf(shownExponent)}`;
}

export function superscriptOf(value: number): string {
    const digits = [...String(Math.abs(Math.round(value)))].map((digit) => SUPERSCRIPT_DIGITS[Number(digit)]).join('');

    return value < 0 ? SUPERSCRIPT_MINUS + digits : digits;
}
