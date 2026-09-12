import { Color, Vector3 } from "three";
import { ElementSummary } from "../../../data/elements/element";
import { BondType, MatterState, SubstanceDetail, SubstanceKind } from "../../../data/substances/substance";
import { BondKind, LayoutUnit, ringPositions, UnitAtom, UnitBond } from "../../../engine/scene/bench-layout";
import { WorkspaceItem } from "../../../state/workspace-store";
import { parseHillFormula } from "./hill-formula";
import { Phase } from "../../../engine/core/matter";

type Elements = ReadonlyMap<string, ElementSummary>;

interface Unit {
    readonly atoms: readonly UnitAtom[];
    readonly bonds: readonly UnitBond[];
}

const PHASES: Readonly<Record<MatterState, Phase>> = {
    Solid: 'solid',
    Liquid: 'liquid',
    Gas: 'gas',
    Aqueous: 'aqueous',
    Plasma: 'plasma'
};

const BOND_KINDS: Readonly<Record<BondType, BondKind>> = {
    Single: 'single',
    Double: 'double',
    Triple: 'triple',
    Aromatic: 'aromatic',
    Ionic: 'ionic',
    Hydrogen: 'hydrogen',
    Metallic: 'metallic'
};

const UNIT_BONDS: Readonly<Record<SubstanceKind, BondKind>> = {
    Molecular: 'single',
    Ionic: 'ionic',
    Metallic: 'metallic',
    Monatomic: 'single',
    NetworkCovalent: 'single'
};

const BALL_BASE = 0.22;
const BALL_SCALE = 0.32;
const FALLBACK_COVALENT = 0.75;
const FALLBACK_COLOR = '#909090';
const MAX_COPIES = 8;

export function buildBenchUnits(
    entries: readonly WorkspaceItem[],
    details: ReadonlyMap<string, SubstanceDetail>,
    elements: readonly ElementSummary[]
): LayoutUnit[] {
    if (elements.length === 0) {
        return [];
    }

    const bySymbol: Elements = new Map(elements.map((element) => [element.symbol, element]));
    const units: LayoutUnit[] = [];

    for (const entry of entries) {
        const detail = details.get(entry.substance.id);

        if (!detail) {
            continue;
        }

        const { atoms, bonds } = unitOf(detail, bySymbol);

        for (let copy = 0; copy < Math.min(entry.count, MAX_COPIES); copy++) {
            units.push({ id: `${detail.id}#${copy}`, substanceId: detail.id, atoms, bonds });
        }
    }

    return units;
}

function unitOf(detail: SubstanceDetail, elements: Elements): Unit {
    const phase = PHASES[detail.stateAtRoomTemperature];

    if (detail.structure) {
        return {
            atoms: detail.structure.atoms.map((atom) => describe(atom.symbol, new Vector3(atom.x, atom.y, atom.z), phase, elements)),
            bonds: detail.structure.bonds.map((bond) => ({ from: bond.fromAtomIndex, to: bond.toAtomIndex, kind: BOND_KINDS[bond.type] }))
        };
    }

    const symbols = parseHillFormula(detail.hillFormula).flatMap(({ symbol, count }) => Array<string>(count).fill(symbol));
    const positions = ringPositions(symbols.map((symbol) => radiusOf(elements.get(symbol))));

    return {
        atoms: symbols.map((symbol, index) => describe(symbol, positions[index], phase, elements)),
        bonds: ringBonds(symbols.length, UNIT_BONDS[detail.kind])
    };
}

function ringBonds(count: number, kind: BondKind): UnitBond[] {
    if (count < 2) {
        return [];
    }

    if (count === 2) {
        return [{ from: 0, to: 1, kind }];
    }

    return Array.from({ length: count }, (_, index) => ({ from: index, to: (index + 1) % count, kind }));
}

function describe(symbol: string, position: Vector3, phase: Phase, elements: Elements): UnitAtom {
    const element = elements.get(symbol);

    return { symbol, phase, position, radius: radiusOf(element), color: new Color(element?.displayColor ?? FALLBACK_COLOR) };
}

function radiusOf(element: ElementSummary | undefined): number {
    const covalent = (element?.covalentRadiusPicometers ?? FALLBACK_COVALENT * 100) / 100;

    return BALL_BASE + BALL_SCALE * covalent;
}