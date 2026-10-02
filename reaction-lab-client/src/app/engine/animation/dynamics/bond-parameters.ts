import { BondKind } from "../../scene/bench-layout";

export interface BondParameters {
    readonly dissociationKilojoulesPerMole: number;
    readonly stretchWavenumber: number;
}

const BOND_ORDER_BY_KIND: Readonly<Record<BondKind, number>> = {
    single: 1,
    double: 2,
    triple: 3,
    aromatic: 1.5,
    ionic: 1,
    hydrogen: 0.1,
    metallic: 1
};

const MEASURED_BY_BOND_KEY = new Map<string, BondParameters>([
    ['H-H:1', parameters(436, 4401)],
    ['H-O:1', parameters(463, 3700)],
    ['C-H:1', parameters(413, 2950)],
    ['H-N:1', parameters(391, 3350)],
    ['Cl-H:1', parameters(431, 2990)],
    ['H-S:1', parameters(363, 2600)],
    ['Br-H:1', parameters(366, 2650)],
    ['F-H:1', parameters(567, 4138)],
    ['H-I:1', parameters(299, 2310)],
    ['O-O:1', parameters(146, 880)],
    ['O-O:2', parameters(498, 1580)],
    ['C-O:1', parameters(358, 1100)],
    ['C-O:2', parameters(745, 1900)],
    ['C-O:3', parameters(1072, 2143)],
    ['C-C:1', parameters(348, 1000)],
    ['C-C:2', parameters(614, 1650)],
    ['C-C:3', parameters(839, 2050)],
    ['C-N:1', parameters(293, 1100)],
    ['C-N:3', parameters(891, 2100)],
    ['N-N:1', parameters(163, 1000)],
    ['N-N:2', parameters(418, 1550)],
    ['N-N:3', parameters(945, 2330)],
    ['N-O:1', parameters(201, 1000)],
    ['N-O:2', parameters(607, 1600)],
    ['Cl-Cl:1', parameters(242, 560)],
    ['O-S:2', parameters(523, 1150)]
]);

const TYPICAL_BY_KIND: Readonly<Record<BondKind, BondParameters>> = {
    single: parameters(350, 1100),
    double: parameters(600, 1600),
    triple: parameters(900, 2100),
    aromatic: parameters(480, 1400),
    ionic: parameters(350, 1100),
    hydrogen: parameters(20, 200),
    metallic: parameters(350, 1100)
};

export function bondParametersFor(firstSymbol: string, secondSymbol: string, kind: BondKind): BondParameters {
    const elementPair = firstSymbol < secondSymbol ? `${firstSymbol}-${secondSymbol}` : `${secondSymbol}-${firstSymbol}`;

    return MEASURED_BY_BOND_KEY.get(`${elementPair}:${BOND_ORDER_BY_KIND[kind]}`) ?? TYPICAL_BY_KIND[kind];
}

function parameters(dissociationKilojoulesPerMole: number, stretchWavenumber: number): BondParameters {
    return { dissociationKilojoulesPerMole, stretchWavenumber };
}