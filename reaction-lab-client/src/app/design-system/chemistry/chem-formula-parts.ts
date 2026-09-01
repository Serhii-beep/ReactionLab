export type FormulaPartKind = 'text' | 'subscript';

export interface FormulaPart {
    readonly kind: FormulaPartKind;
    readonly value: string;
}

const SUBSCRIPTABLE = /[a-z)\]]/i;

export function formulaParts(formula: string): readonly FormulaPart[] {
    const parts: FormulaPart[] = [];

    for (const run of formula.match(/\d+|\D+/g) ?? []) {
        const previous = parts.at(-1);
        const subscript = /^\d/.test(run) && previous !== undefined && SUBSCRIPTABLE.test(previous.value.at(-1) ?? '');

        parts.push({ kind: subscript ? 'subscript' : 'text', value: run });
    }

    return parts;
}

export function formatCharge(charge: number): string {
    if (charge === 0) {
        return '';
    }

    const magnitude = Math.abs(charge);
    const sign = charge < 0 ? '\u2212' : '+';

    return magnitude === 1 ? sign : `${magnitude}${sign}`;
}