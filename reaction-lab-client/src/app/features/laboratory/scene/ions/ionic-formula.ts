import { ION_SPECIES, IonSpecies } from "./ion-table";

export interface IonCount {
    readonly species: IonSpecies;
    readonly charge: number;
    readonly count: number;
}

export interface IonicFormula {
    readonly cations: readonly IonCount[];
    readonly anions: readonly IonCount[];
}

interface WrittenIon {
    readonly species: IonSpecies;
    readonly count: number;
}

interface IonMatch {
    readonly species: IonSpecies;
    readonly after: string;
}

const SPECIES_LONGEST_FIRST = [...ION_SPECIES].sort((first, second) => second.token.length - first.token.length);
const SPECIES_BY_TOKEN: ReadonlyMap<string, IonSpecies> = new Map(ION_SPECIES.map((species) => [species.token, species]));
const LEADING_COUNT = /^\d*/;

export function parseIonicFormula(formula: string): IonicFormula | null {
    const written = writtenIonsOf(formula);

    if (written === null) {
        return null;
    }

    const cations = written.filter((ion) => ion.species.role === 'cation');
    const anions = written.filter((ion) => ion.species.role === 'anion');

    return cations.length === 0 || anions.length === 0 ? null : balanced(cations, anions);
}

export function radiusAngstromOf(ion: IonCount): number {
    return ion.species.radiusAngstromByCharge.get(ion.charge) ?? 0;
}

function writtenIonsOf(formula: string): WrittenIon[] | null {
    const written: WrittenIon[] = [];
    let rest = formula;

    while (rest.length > 0) {
        const match = rest.startsWith('(') ? bracketedIonAt(rest) : plainIonAt(rest);

        if (match === null) {
            return null;
        }

        const digits = LEADING_COUNT.exec(match.after)?.[0] ?? '';

        written.push({ species: match.species, count: digits === '' ? 1 : Number(digits) });
        rest = match.after.slice(digits.length);
    }

    return written;
}

function bracketedIonAt(text: string): IonMatch | null {
    const close = text.indexOf(')');
    const species = close < 0 ? undefined : SPECIES_BY_TOKEN.get(text.slice(1, close));

    return species === undefined ? null : { species, after: text.slice(close + 1) };
}

function plainIonAt(text: string): IonMatch | null {
    const species = SPECIES_LONGEST_FIRST.find((candidate) => text.startsWith(candidate.token));

    return species === undefined ? null : { species, after: text.slice(species.token.length) };
}

function balanced(cations: readonly WrittenIon[], anions: readonly WrittenIon[]): IonicFormula | null {
    const anionCounts = anions.map((ion) => ({ ...ion, charge: firstChargeOf(ion.species) }));
    const negative = totalChargeOf(anionCounts);
    const fixed = cations.filter((ion) => ion.species.radiusAngstromByCharge.size === 1).map((ion) => ({ ...ion, charge: firstChargeOf(ion.species) }));
    const variable = cations.filter((ion) => ion.species.radiusAngstromByCharge.size > 1);

    if (variable.length === 0) {
        return totalChargeOf(fixed) === negative ? { cations: fixed, anions: anionCounts } : null;
    }

    const charge = (negative - totalChargeOf(fixed)) / variable[0].count;

    if (variable.length > 1 || !variable[0].species.radiusAngstromByCharge.has(charge)) {
        return null;
    }

    return { cations: cations.map((ion) => ({ ...ion, charge: ion === variable[0] ? charge : firstChargeOf(ion.species) })), anions: anionCounts };
}

function firstChargeOf(species: IonSpecies): number {
    return [...species.radiusAngstromByCharge.keys()][0];
}

function totalChargeOf(ions: readonly IonCount[]): number {
    return ions.reduce((sum, ion) => sum + ion.charge * ion.count, 0);
}
