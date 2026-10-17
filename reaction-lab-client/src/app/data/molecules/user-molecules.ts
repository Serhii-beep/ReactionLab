import { BondType, SubstanceDetail, SubstanceSummary } from "../substances/substance";
import { BondOrder } from "./molecule-sketch";
import { SavedMolecule } from "./saved-molecule";

const USER_MOLECULE_PREFIX = 'molecule:';
const REVISION_SEPARATOR = '@';
const CARBON = 'C';

const BOND_TYPES: Readonly<Record<BondOrder, BondType>> = {
    1: 'Single',
    2: 'Double',
    3: 'Triple'
};

export function userMoleculeSubstanceId(molecule: SavedMolecule): string {
    return USER_MOLECULE_PREFIX + molecule.id + REVISION_SEPARATOR + molecule.savedAt;
}

export function isUserMoleculeId(substanceId: string): boolean {
    return substanceId.startsWith(USER_MOLECULE_PREFIX);
}

export function userMoleculeSummary(molecule: SavedMolecule): SubstanceSummary {
    return {
        id: userMoleculeSubstanceId(molecule),
        formula: molecule.hillFormula,
        name: molecule.name,
        kind: 'Molecular',
        isOrganic: molecule.sketch.atoms.some((atom) => atom.symbol === CARBON),
        stateAtRoomTemperature: null,
        weightGramsPerMole: molecule.gramsPerMole,
        category: null
    };
}

export function userMoleculeDetail(molecule: SavedMolecule): SubstanceDetail {
    const positions = molecule.positionsAngstroms;

    return {
        ...userMoleculeSummary(molecule),
        hillFormula: molecule.hillFormula,
        iupacName: null,
        description: null,
        safetyInformation: null,
        commonNames: [],
        uses: [],
        interestingFacts: [],
        structure: {
            atoms: molecule.sketch.atoms.map((atom, index) => ({
                symbol: atom.symbol,
                x: positions[3 * index],
                y: positions[3 * index + 1],
                z: positions[3 * index + 2]
            })),
            bonds: molecule.sketch.bonds.map((bond) => ({
                fromAtomIndex: bond.from,
                toAtomIndex: bond.to,
                type: BOND_TYPES[bond.order]
            }))
        }
    };
}
