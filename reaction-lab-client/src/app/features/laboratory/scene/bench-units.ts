import { Color, Vector3 } from "three";
import { ElementSummary } from "../../../data/elements/element";
import { MatterState, SubstanceDetail } from "../../../data/substances/substance";
import { Phase } from "../../../engine/resources/material-cache";
import { LayoutUnit, ringPositions, UnitAtom } from "../../../engine/scene/bench-layout";
import { WorkspaceItem } from "../../../state/workspace-store";
import { parseHillFormula } from "./hill-formula";

type Elements = ReadonlyMap<string, ElementSummary>;

const PHASES: Readonly<Record<MatterState, Phase>> = {
    Solid: 'solid',
    Liquid: 'liquid',
    Gas: 'gas',
    Aqueous: 'aqueous',
    Plasma: 'plasma'
};

const RADIUS_SCALE = 1.1;
const FALLBACK_RADIUS = 0.75;
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

        const atoms = unitAtoms(detail, bySymbol);

        for (let copy = 0; copy < Math.min(entry.count, MAX_COPIES); copy++) {
            units.push({ id: `${detail.id}#${copy}`, substanceId: detail.id, atoms });
        }
    }

    return units;
}

function unitAtoms(detail: SubstanceDetail, elements: Elements): readonly UnitAtom[] {
    const phase = PHASES[detail.stateAtRoomTemperature];

    if (detail.structure) {
        return detail.structure.atoms.map((atom) => describe(atom.symbol, new Vector3(atom.x, atom.y, atom.z), phase, elements));
    }

    const symbols = parseHillFormula(detail.hillFormula).flatMap(({ symbol, count }) =>
        Array<string>(count).fill(symbol));
    const positions = ringPositions(symbols.map((symbol) => radiusOf(elements.get(symbol))));

    return symbols.map((symbol, index) => describe(symbol, positions[index], phase, elements));
}

function describe(symbol: string, position: Vector3, phase: Phase, elements: Elements): UnitAtom {
    const element = elements.get(symbol);

    return { symbol, phase, position, radius: radiusOf(element), color: new Color(element?.displayColor ?? FALLBACK_COLOR) };
}

function radiusOf(element: ElementSummary | undefined): number {
    const picometers = element?.covalentRadiusPicometers ?? FALLBACK_RADIUS * 100;

    return (picometers / 100) * RADIUS_SCALE;
}