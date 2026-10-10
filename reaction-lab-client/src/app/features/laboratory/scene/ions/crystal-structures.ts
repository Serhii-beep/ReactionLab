import { IonCount, IonicFormula, radiusAngstromOf } from "./ionic-formula";

export type CrystalStructure =
    | 'rock-salt'
    | 'cesium-chloride'
    | 'zinc-blende'
    | 'fluorite'
    | 'antifluorite'
    | 'layered'
    | 'corundum'
    | 'aluminum-chloride'
    | 'anti-bixbyite'
    | 'filled-fluorite';

const OCTAHEDRAL_RADIUS_RATIO = 0.414;
const CUBIC_RADIUS_RATIO = 0.732;

const STRUCTURE_BY_FORMULA: ReadonlyMap<string, CrystalStructure> = new Map<string, CrystalStructure>([
    ...['NaCl', 'KCl', 'LiCl', 'KI', 'AgCl', 'MgO', 'CaO', 'BaO', 'FeO', 'PbS', 'CaS', 'BaS', 'MgS', 'FeS'].map((formula) => [formula, 'rock-salt'] as const),
    ...['CaCO3', 'MgCO3', 'FeCO3', 'ZnCO3', 'NaNO3', 'LiNO3'].map((formula) => [formula, 'rock-salt'] as const),
    ['NH4Cl', 'cesium-chloride'],
    ...['ZnS', 'AgI', 'ZnO', 'AlN'].map((formula) => [formula, 'zinc-blende'] as const),
    ...['Ba(NO3)2', 'Ca(NO3)2', 'Pb(NO3)2'].map((formula) => [formula, 'fluorite'] as const),
    ...['PbI2', 'Mg(OH)2', 'Ca(OH)2', 'Fe(OH)2', 'MgCl2', 'FeCl2'].map((formula) => [formula, 'layered'] as const)
]);

export function crystalStructureOf(formula: string, ions: IonicFormula): CrystalStructure {
    return STRUCTURE_BY_FORMULA.get(formula) ?? structureByRatioOf(ions);
}

function structureByRatioOf({ cations, anions }: IonicFormula): CrystalStructure {
    const cationCount = ionCountOf(cations);
    const anionCount = ionCountOf(anions);
    const radiusRatio = meanRadiusOf(cations) / meanRadiusOf(anions);

    if (cationCount === anionCount) {
        return radiusRatio >= OCTAHEDRAL_RADIUS_RATIO ? 'rock-salt' : 'zinc-blende';
    }

    if (anionCount === 2 * cationCount) {
        return radiusRatio >= CUBIC_RADIUS_RATIO ? 'fluorite' : 'layered';
    }

    return structureByCountsOf(cationCount, anionCount);
}

function structureByCountsOf(cationCount: number, anionCount: number): CrystalStructure {
    if (cationCount === 2 * anionCount) {
        return 'antifluorite';
    }

    if (2 * anionCount === 3 * cationCount) {
        return 'corundum';
    }

    if (anionCount === 3 * cationCount) {
        return 'aluminum-chloride';
    }

    return 2 * cationCount === 3 * anionCount ? 'anti-bixbyite' : 'filled-fluorite';
}

function ionCountOf(ions: readonly IonCount[]): number {
    return ions.reduce((sum, ion) => sum + ion.count, 0);
}

function meanRadiusOf(ions: readonly IonCount[]): number {
    return ions.reduce((sum, ion) => sum + radiusAngstromOf(ion) * ion.count, 0) / ionCountOf(ions);
}
