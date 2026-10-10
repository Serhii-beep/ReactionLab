import { PlacedAtom, PlacedBond } from "./bench-layout";

interface PlacedIon {
    readonly charge: number;
    readonly atoms: readonly PlacedAtom[];
}

interface IonPair {
    readonly cation: PlacedIon;
    readonly anion: PlacedIon;
    readonly separation: number;
}

const CONTACT_TOLERANCE = 1.2;

export function latticeContactsOf(atoms: readonly PlacedAtom[], bonds: readonly PlacedBond[]): PlacedBond[] {
    const pairs = oppositePairsOf(ionsOf(atoms));
    const nearest = pairs.reduce((shortest, pair) => Math.min(shortest, pair.separation), Infinity);
    const partnersByAtom = partnersByAtomOf(bonds);

    return pairs
        .filter((pair) => pair.separation <= nearest * CONTACT_TOLERANCE && !alreadyBonded(partnersByAtom, pair))
        .map((pair) => contactBondOf(pair));
}

function ionsOf(atoms: readonly PlacedAtom[]): PlacedIon[] {
    const atomsByIon = new Map<string, PlacedAtom[]>();

    for (const atom of atoms) {
        if (atom.ion !== null) {
            const key = `${atom.unitId}|${atom.ion.index}`;

            atomsByIon.set(key, [...(atomsByIon.get(key) ?? []), atom]);
        }
    }

    return [...atomsByIon.values()].map((ionAtoms) => ({ charge: ionAtoms[0].ion?.charge ?? 0, atoms: ionAtoms }));
}

function oppositePairsOf(ions: readonly PlacedIon[]): IonPair[] {
    const cations = ions.filter((ion) => ion.charge > 0);
    const anions = ions.filter((ion) => ion.charge < 0);

    return cations.flatMap((cation) => anions.map((anion) => ({ cation, anion, separation: cation.atoms[0].position.distanceTo(anion.atoms[0].position) })));
}

function partnersByAtomOf(bonds: readonly PlacedBond[]): ReadonlyMap<PlacedAtom, ReadonlySet<PlacedAtom>> {
    const partners = new Map<PlacedAtom, Set<PlacedAtom>>();

    for (const { from, to } of bonds) {
        partners.set(from, (partners.get(from) ?? new Set()).add(to));
        partners.set(to, (partners.get(to) ?? new Set()).add(from));
    }

    return partners;
}

function alreadyBonded(partnersByAtom: ReadonlyMap<PlacedAtom, ReadonlySet<PlacedAtom>>, { cation, anion }: IonPair): boolean {
    return cation.atoms.some((atom) => anion.atoms.some((other) => partnersByAtom.get(atom)?.has(other) ?? false));
}

function contactBondOf({ cation, anion }: IonPair): PlacedBond {
    let from = cation.atoms[0];
    let to = anion.atoms[0];

    for (const cationAtom of cation.atoms) {
        for (const anionAtom of anion.atoms) {
            if (cationAtom.position.distanceTo(anionAtom.position) < from.position.distanceTo(to.position)) {
                from = cationAtom;
                to = anionAtom;
            }
        }
    }

    return { from, to, kind: 'ionic', sidePoint: from.position.clone().add(to.position).multiplyScalar(0.5), strength: 1, partial: false };
}
