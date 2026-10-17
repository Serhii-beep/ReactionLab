import { Color } from "three";
import { BondKind, LayoutUnit, UnitAtom, UnitBond } from "../../../engine/scene/bench-layout";
import { Phase } from "../../../engine/core/matter";
import { BondOrder } from "../../../data/molecules/molecule-sketch";
import { colorOf, covalentBallRadiusOf, ElementsBySymbol } from "../scene/atom-balls";
import { PlacedSketch } from "./sketch-geometry";

export type SketchPickTarget =
    | { readonly kind: 'atom'; readonly key: number }
    | { readonly kind: 'openValence'; readonly ownerKey: number; readonly stub: number };

export const SKETCH_UNIT_ID = 'sketch';

const SKETCH_PHASE: Phase = 'gas';
const OPEN_VALENCE_RADIUS = 0.12;
const ATOM_PICK = 'atom:';
const OPEN_VALENCE_PICK = 'open:';
const PICK_SEPARATOR = ':';

const BOND_KINDS: Readonly<Record<BondOrder, BondKind>> = {
    1: 'single',
    2: 'double',
    3: 'triple'
};

export function sketchUnit(placed: PlacedSketch, elements: ElementsBySymbol, openValenceColor: Color): LayoutUnit {
    const atoms: UnitAtom[] = placed.sketch.atoms.map((atom, index) => {
        const element = elements.get(atom.symbol);

        return {
            symbol: atom.symbol,
            phase: SKETCH_PHASE,
            position: placed.positions[index],
            radius: covalentBallRadiusOf(element),
            color: colorOf(element),
            ion: null,
            pickId: atomPickId(atom.key)
        };
    });
    const openValences: UnitAtom[] = placed.openValencePositions.map((position, stub) => ({
        symbol: '',
        phase: SKETCH_PHASE,
        position,
        radius: OPEN_VALENCE_RADIUS,
        color: openValenceColor,
        ion: null,
        pickId: openValencePickId(placed.sketch.atoms[placed.openValenceOwners[stub]].key, stub)
    }));
    const bonds: UnitBond[] = [
        ...placed.sketch.bonds.map((bond) => ({ from: bond.from, to: bond.to, kind: BOND_KINDS[bond.order] })),
        ...placed.openValenceOwners.map((owner, stub) => ({ from: owner, to: atoms.length + stub, kind: 'hydrogen' as const }))
    ];

    return { id: SKETCH_UNIT_ID, substanceId: SKETCH_UNIT_ID, atoms: [...atoms, ...openValences], bonds, crystallite: false };
}

export function atomPickId(key: number): string {
    return ATOM_PICK + key;
}

export function openValencePickId(ownerKey: number, stub: number): string {
    return OPEN_VALENCE_PICK + ownerKey + PICK_SEPARATOR + stub;
}

export function pickTargetOf(pickId: string | undefined): SketchPickTarget | null {
    if (pickId?.startsWith(ATOM_PICK)) {
        return { kind: 'atom', key: Number(pickId.slice(ATOM_PICK.length)) };
    }

    if (pickId?.startsWith(OPEN_VALENCE_PICK)) {
        const [ownerKey, stub] = pickId.slice(OPEN_VALENCE_PICK.length).split(PICK_SEPARATOR).map(Number);

        return { kind: 'openValence', ownerKey, stub };
    }

    return null;
}
