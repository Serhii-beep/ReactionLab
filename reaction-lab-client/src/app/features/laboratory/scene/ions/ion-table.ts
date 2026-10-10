import { acetate, bicarbonate, diatomic, hydroxide, IonGeometry, monatomic, tetrahedral, trigonal } from "./ion-geometry";

export type IonRole = 'cation' | 'anion';

export interface IonSpecies {
    readonly token: string;
    readonly role: IonRole;
    readonly radiusAngstromByCharge: ReadonlyMap<number, number>;
    readonly geometry: IonGeometry;
}

export const ION_SPECIES: readonly IonSpecies[] = [
    monatomicIon('Li', 'cation', [[1, 0.76]]),
    monatomicIon('Na', 'cation', [[1, 1.02]]),
    monatomicIon('K', 'cation', [[1, 1.38]]),
    monatomicIon('Ag', 'cation', [[1, 1.15]]),
    monatomicIon('Mg', 'cation', [[2, 0.72]]),
    monatomicIon('Ca', 'cation', [[2, 1]]),
    monatomicIon('Ba', 'cation', [[2, 1.35]]),
    monatomicIon('Zn', 'cation', [[2, 0.74]]),
    monatomicIon('Cu', 'cation', [[2, 0.73], [1, 0.77]]),
    monatomicIon('Fe', 'cation', [[2, 0.78], [3, 0.645]]),
    monatomicIon('Pb', 'cation', [[2, 1.19]]),
    monatomicIon('Al', 'cation', [[3, 0.535]]),
    polyatomicIon('NH4', 'cation', [1, 1.48], tetrahedral('N', 'H', 1.03, 'single')),
    monatomicIon('F', 'anion', [[1, 1.33]]),
    monatomicIon('Cl', 'anion', [[1, 1.81]]),
    monatomicIon('Br', 'anion', [[1, 1.96]]),
    monatomicIon('I', 'anion', [[1, 2.2]]),
    monatomicIon('O', 'anion', [[2, 1.4]]),
    monatomicIon('S', 'anion', [[2, 1.84]]),
    monatomicIon('N', 'anion', [[3, 1.46]]),
    polyatomicIon('OH', 'anion', [1, 1.37], hydroxide()),
    polyatomicIon('NO3', 'anion', [1, 1.79], trigonal('N', 'O', 1.25)),
    polyatomicIon('CO3', 'anion', [2, 1.78], trigonal('C', 'O', 1.29)),
    polyatomicIon('HCO3', 'anion', [1, 1.56], bicarbonate()),
    polyatomicIon('SO4', 'anion', [2, 2.3], tetrahedral('S', 'O', 1.49, 'aromatic')),
    polyatomicIon('PO4', 'anion', [3, 2.38], tetrahedral('P', 'O', 1.54, 'aromatic')),
    polyatomicIon('MnO4', 'anion', [1, 2.4], tetrahedral('Mn', 'O', 1.63, 'aromatic')),
    polyatomicIon('ClO', 'anion', [1, 1.72], diatomic('Cl', 'O', 1.69)),
    polyatomicIon('CH3COO', 'anion', [1, 1.62], acetate())
];

function monatomicIon(symbol: string, role: IonRole, radiusAngstromByCharge: readonly [number, number][]): IonSpecies {
    return { token: symbol, role, radiusAngstromByCharge: new Map(radiusAngstromByCharge), geometry: monatomic(symbol) };
}

function polyatomicIon(token: string, role: IonRole, radiusAngstromByCharge: [number, number], geometry: IonGeometry): IonSpecies {
    return { token, role, radiusAngstromByCharge: new Map([radiusAngstromByCharge]), geometry };
}
