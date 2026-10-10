import { Vector3 } from "three";
import { ElementSummary } from "../../../data/elements/element";
import { BondType, MatterState, SubstanceDetail, SubstanceKind } from "../../../data/substances/substance";
import { BondKind, LayoutUnit, ringPositions, UnitAtom, UnitBond } from "../../../engine/scene/bench-layout";
import { WorkspaceItem } from "../../../state/workspace-store";
import { parseHillFormula } from "./hill-formula";
import { Phase } from "../../../engine/core/matter";
import { colorOf, covalentBallRadiusOf, ElementsBySymbol } from "./atom-balls";
import { ionicUnitsOf } from "./ionic-units";

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

export const MAX_COPIES = 64;

export function buildBenchUnits(
    entries: readonly WorkspaceItem[],
    details: ReadonlyMap<string, SubstanceDetail>,
    elements: readonly ElementSummary[]
): LayoutUnit[] {
    if (elements.length === 0) {
        return [];
    }

    const bySymbol: ElementsBySymbol = new Map(elements.map((element) => [element.symbol, element]));
    const units: LayoutUnit[] = [];

    for (const entry of entries) {
        const detail = details.get(entry.substance.id);

        if (!detail) {
            continue;
        }

        const copies = Math.min(entry.count, MAX_COPIES);
        const phase = PHASES[detail.stateAtRoomTemperature];

        units.push(...(ionicUnitsOf(detail, copies, phase, bySymbol) ?? copiesOf(detail, unitOf(detail, phase, bySymbol), copies)));
    }

    return units;
}

function copiesOf(detail: SubstanceDetail, unit: Unit, copies: number): LayoutUnit[] {
    return Array.from({ length: copies }, (_, copy) => ({ id: `${detail.id}#${copy}`, substanceId: detail.id, ...unit, crystallite: false }));
}

function unitOf(detail: SubstanceDetail, phase: Phase, elements: ElementsBySymbol): Unit {
    if (detail.structure) {
        return {
            atoms: detail.structure.atoms.map((atom) => describe(atom.symbol, new Vector3(atom.x, atom.y, atom.z), phase, elements)),
            bonds: detail.structure.bonds.map((bond) => ({ from: bond.fromAtomIndex, to: bond.toAtomIndex, kind: BOND_KINDS[bond.type] }))
        };
    }

    const symbols = parseHillFormula(detail.hillFormula).flatMap(({ symbol, count }) => Array<string>(count).fill(symbol));
    const positions = ringPositions(symbols.map((symbol) => covalentBallRadiusOf(elements.get(symbol))));

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

function describe(symbol: string, position: Vector3, phase: Phase, elements: ElementsBySymbol): UnitAtom {
    const element = elements.get(symbol);

    return { symbol, phase, position, radius: covalentBallRadiusOf(element), color: colorOf(element), ion: null };
}
