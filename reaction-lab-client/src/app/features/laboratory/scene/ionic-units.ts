import { SubstanceDetail } from "../../../data/substances/substance";
import { LayoutUnit, UnitAtom, UnitBond } from "../../../engine/scene/bench-layout";
import { Phase } from "../../../engine/core/matter";
import { ballRadiusOf, colorOf, covalentBallRadiusOf, ElementsBySymbol } from "./atom-balls";
import { IonicFormula, parseIonicFormula, radiusAngstromOf } from "./ions/ionic-formula";
import { crystalStructureOf } from "./ions/crystal-structures";
import { crystalliteOf, LatticeIon } from "./ions/crystal-lattice";
import { IonRole } from "./ions/ion-table";

export interface IonicUnit {
    readonly atoms: readonly UnitAtom[];
    readonly bonds: readonly UnitBond[];
}

interface IonAtoms {
    readonly role: IonRole;
    readonly atomIndices: readonly number[];
}

interface ContactLink {
    readonly ionIndex: number;
    readonly from: number;
    readonly to: number;
    readonly distance: number;
}

const LATTICE_BY_FORMULA_AND_COUNT = new Map<string, readonly (readonly LatticeIon[])[]>();

export function ionicFormulaOf(detail: SubstanceDetail): IonicFormula | null {
    return detail.kind === 'Ionic' ? parseIonicFormula(detail.formula) : null;
}

export function ionicUnitsOf(detail: SubstanceDetail, copies: number, phase: Phase, elements: ElementsBySymbol): LayoutUnit[] | null {
    const formula = ionicFormulaOf(detail);

    if (formula === null) {
        return null;
    }

    return latticeOf(detail.formula, formula, copies).map((ions, copy) => ({
        id: `${detail.id}#${copy}`,
        substanceId: detail.id,
        ...unitOfIons(ions, phase, elements),
        crystallite: true
    }));
}

export function latticeOf(writtenFormula: string, formula: IonicFormula, unitCount: number): readonly (readonly LatticeIon[])[] {
    const key = `${writtenFormula}|${unitCount}`;
    const cached = LATTICE_BY_FORMULA_AND_COUNT.get(key);

    if (cached !== undefined) {
        return cached;
    }

    const lattice = crystalliteOf(formula, crystalStructureOf(writtenFormula, formula), unitCount);

    LATTICE_BY_FORMULA_AND_COUNT.set(key, lattice);

    return lattice;
}

export function unitOfIons(ions: readonly LatticeIon[], phase: Phase, elements: ElementsBySymbol): IonicUnit {
    const atoms: UnitAtom[] = [];
    const bonds: UnitBond[] = [];
    const ionAtoms: IonAtoms[] = [];

    ions.forEach((latticeIon, index) => {
        const first = atoms.length;
        const { geometry } = latticeIon.ion.species;
        const monatomic = geometry.atoms.length === 1;
        const charge = latticeIon.role === 'cation' ? latticeIon.ion.charge : -latticeIon.ion.charge;

        for (const member of geometry.atoms) {
            const element = elements.get(member.symbol);

            atoms.push({
                symbol: member.symbol,
                phase,
                position: member.position.clone().add(latticeIon.position),
                radius: monatomic ? ballRadiusOf(radiusAngstromOf(latticeIon.ion)) : covalentBallRadiusOf(element),
                color: colorOf(element),
                ion: { index, charge }
            });
        }

        bonds.push(...geometry.bonds.map((bond) => ({ from: bond.from + first, to: bond.to + first, kind: bond.kind })));
        ionAtoms.push({ role: latticeIon.role, atomIndices: geometry.atoms.map((_, offset) => first + offset) });
    });

    return { atoms, bonds: [...bonds, ...contactBondsOf(ionAtoms, atoms)] };
}

function contactBondsOf(ions: readonly IonAtoms[], atoms: readonly UnitAtom[]): UnitBond[] {
    const joined = new Set<number>([0]);
    const bonds: UnitBond[] = [];

    while (joined.size < ions.length) {
        const link = shortestLinkFrom(joined, ions, atoms);

        if (link === null) {
            break;
        }

        joined.add(link.ionIndex);
        bonds.push({ from: link.from, to: link.to, kind: 'ionic' });
    }

    return bonds;
}

function shortestLinkFrom(joined: ReadonlySet<number>, ions: readonly IonAtoms[], atoms: readonly UnitAtom[]): ContactLink | null {
    let shortest: ContactLink | null = null;

    for (let ionIndex = 0; ionIndex < ions.length; ionIndex++) {
        for (const joinedIndex of joined) {
            if (joined.has(ionIndex) || ions[joinedIndex].role === ions[ionIndex].role) {
                continue;
            }

            const link = closestPairOf(ions[joinedIndex], ions[ionIndex], atoms, ionIndex);

            if (shortest === null || link.distance < shortest.distance) {
                shortest = link;
            }
        }
    }

    return shortest;
}

function closestPairOf(joinedIon: IonAtoms, newIon: IonAtoms, atoms: readonly UnitAtom[], ionIndex: number): ContactLink {
    const [cation, anion] = joinedIon.role === 'cation' ? [joinedIon, newIon] : [newIon, joinedIon];
    let closest: ContactLink = { ionIndex, from: cation.atomIndices[0], to: anion.atomIndices[0], distance: Infinity };

    for (const from of cation.atomIndices) {
        for (const to of anion.atomIndices) {
            const distance = atoms[from].position.distanceTo(atoms[to].position);

            if (distance < closest.distance) {
                closest = { ionIndex, from, to, distance };
            }
        }
    }

    return closest;
}
