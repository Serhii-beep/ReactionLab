import { SketchAtomDescription, SketchDescription } from "../../../core/chemistry/chemistry-contracts";
import { MoleculeSketch } from "../../../data/molecules/molecule-sketch";
import { formatCharge } from "../../../design-system/chemistry/chem-formula-parts";
import { PlacedSketch } from "./sketch-geometry";

export type BuilderStatusTone = 'neutral' | 'warning' | 'danger' | 'success';

export interface BuilderStatus {
    readonly key: string;
    readonly params: Readonly<Record<string, string | number>>;
    readonly tone: BuilderStatusTone;
}

export interface SelectedAtomSummary {
    readonly number: number;
    readonly symbol: string;
    readonly neighbors: readonly string[];
    readonly formalCharge: number;
    readonly chargeText: string;
    readonly inferred: boolean;
    readonly explicit: boolean;
    readonly lonePairs: number;
    readonly openValence: number;
}

export function builderStatusOf(
    empty: boolean,
    description: SketchDescription | null,
    failed: boolean,
    connected: boolean
): BuilderStatus | null {
    if (failed) {
        return { key: 'failed', params: {}, tone: 'danger' };
    }

    if (empty) {
        return { key: 'empty', params: {}, tone: 'neutral' };
    }

    if (description === null) {
        return null;
    }

    return structureStatusOf(description, connected);
}

export function selectedAtomSummaryOf(
    index: number | null,
    sketch: MoleculeSketch,
    placed: PlacedSketch | null
): SelectedAtomSummary | null {
    const atom = index === null ? undefined : sketch.atoms[index];

    if (index === null || atom === undefined) {
        return null;
    }

    const described = describedAtom(atom.key, placed);
    const formalCharge = atom.formalCharge ?? described.formalCharge;

    return {
        number: index + 1,
        symbol: atom.symbol,
        neighbors: neighborSymbolsOf(index, sketch),
        formalCharge,
        chargeText: formatCharge(formalCharge),
        inferred: atom.formalCharge === null && described.formalChargeInferred,
        explicit: atom.formalCharge !== null,
        lonePairs: described.lonePairs,
        openValence: described.openValence
    };
}

function describedAtom(key: number, placed: PlacedSketch | null): SketchAtomDescription {
    const placedIndex = placed?.sketch.atoms.findIndex((candidate) => candidate.key === key) ?? -1;

    return placed !== null && placedIndex >= 0
        ? placed.description.atoms[placedIndex]
        : { formalCharge: 0, formalChargeInferred: false, lonePairs: 0, openValence: 0, status: 'Unchecked' };
}

function neighborSymbolsOf(index: number, sketch: MoleculeSketch): readonly string[] {
    return sketch.bonds.flatMap((bond) => {
        if (bond.from === index) {
            return [sketch.atoms[bond.to].symbol];
        }

        return bond.to === index ? [sketch.atoms[bond.from].symbol] : [];
    });
}

function structureStatusOf(description: SketchDescription, connected: boolean): BuilderStatus {
    const incomplete = description.atoms.filter((atom) => atom.status === 'NeedsBonds').length;

    if (description.atoms.some((atom) => atom.status === 'Unchecked')) {
        return { key: 'unknownElement', params: {}, tone: 'danger' };
    }

    if (description.atoms.some((atom) => atom.status === 'TooManyBonds')) {
        return { key: 'tooManyBonds', params: {}, tone: 'danger' };
    }

    if (incomplete > 0) {
        return { key: 'incomplete', params: { count: incomplete }, tone: 'warning' };
    }

    if (description.geometryError !== undefined) {
        return { key: 'noGeometry', params: {}, tone: 'warning' };
    }

    if (!connected) {
        return { key: 'fragments', params: {}, tone: 'warning' };
    }

    return description.charge === 0
        ? { key: 'complete', params: {}, tone: 'success' }
        : { key: 'ion', params: { charge: formatCharge(description.charge) }, tone: 'neutral' };
}
