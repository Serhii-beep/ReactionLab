import { Vector3 } from "three";
import { BondKind, PlacedAtom, PlacedBond } from "../scene/bench-layout";
import { AtomPair } from "./atom-pairing";
import { BondChanges } from "./bond-continuity";
import { RecordingIndexByAtom } from "./dynamics-input-builder";
import { DynamicsRecording, recordedPositionAt } from "./dynamics/dynamics-recording";
import { productWeightAt } from "./dynamics/dynamics-schedule";
import { smoothProgressBetween } from "./easing";

export interface RunBondSources {
    readonly bondChanges: BondChanges;
    readonly atomPairs: readonly AtomPair[];
    readonly reactantIndexByAtom: RecordingIndexByAtom;
    readonly productIndexByAtom: RecordingIndexByAtom;
    readonly recording: DynamicsRecording;
}

interface MirroredBond {
    readonly mirror: PlacedBond;
    readonly original: PlacedBond;
}

interface MeasuredBond {
    readonly bond: PlacedBond;
    readonly fromRecordingIndex: number;
    readonly toRecordingIndex: number;
    readonly restLength: number;
}

const PAULING_LENGTH = 0.3;
const MAX_HIDDEN_ORDER = 0.12;
const MIN_SOLID_ORDER = 0.8;
const NEVER_SOLID = Infinity;
const PARTIAL_STRENGTH = { floor: 0.3, perOrder: 0.7 };
const SURVIVING_STRENGTH = { least: 0.5, most: 1.15 };
const LENGTH_WINDOW = { halfWidthSeconds: 0.09, samples: 9 };
const BREAK_LEAD_SECONDS = 0.15;
const OUTSIDE_RECORDING = -1;
const MIN_BOND_LENGTH = 1e-3;
const COVALENT_KINDS: ReadonlySet<BondKind> = new Set<BondKind>(['single', 'double', 'triple', 'aromatic']);

const SAMPLE_OFFSETS_SECONDS = Array.from({ length: LENGTH_WINDOW.samples }, (_, sample) => LENGTH_WINDOW.halfWidthSeconds * ((2 * sample) / (LENGTH_WINDOW.samples - 1) - 1));

export class RunBonds {
    readonly previews: readonly PlacedBond[];
    readonly echoes: readonly PlacedBond[];

    private readonly recording: DynamicsRecording;
    private readonly breaking: readonly MeasuredBond[];
    private readonly forming: readonly MeasuredBond[];
    private readonly survivingBeforeSwap: readonly MeasuredBond[];
    private readonly survivingAfterSwap: readonly MeasuredBond[];
    private readonly measuredPreviews: readonly MeasuredBond[];
    private readonly measuredEchoes: readonly MeasuredBond[];
    private readonly fromPosition = new Vector3();
    private readonly toPosition = new Vector3();

    constructor(sources: RunBondSources) {
        const { bondChanges, atomPairs, reactantIndexByAtom, productIndexByAtom } = sources;
        const reactantByProduct = new Map(atomPairs.map((pair) => [pair.product, pair.reactant]));
        const productByReactant = new Map(atomPairs.map((pair) => [pair.reactant, pair.product]));
        const previewMirrors = bondChanges.forming.flatMap((bond) => mirrorsOf(bond, reactantByProduct));
        const echoMirrors = bondChanges.breaking.flatMap((bond) => mirrorsOf(bond, productByReactant));

        this.recording = sources.recording;
        this.previews = previewMirrors.map(({ mirror }) => mirror);
        this.echoes = echoMirrors.map(({ mirror }) => mirror);
        this.breaking = bondChanges.breaking.map((bond) => measuredOf(bond, bond, reactantIndexByAtom));
        this.forming = bondChanges.forming.map((bond) => measuredOf(bond, bond, productIndexByAtom));
        this.survivingBeforeSwap = bondChanges.surviving.map(({ reactant }) => measuredOf(reactant, reactant, reactantIndexByAtom));
        this.survivingAfterSwap = bondChanges.surviving.map(({ product }) => measuredOf(product, product, productIndexByAtom));
        this.measuredPreviews = previewMirrors.map(({ mirror, original }) => measuredOf(mirror, original, productIndexByAtom));
        this.measuredEchoes = echoMirrors.map(({ mirror, original }) => measuredOf(mirror, original, reactantIndexByAtom));
    }

    showBeforeSwap(seconds: number): void {
        const schedule = this.recording.schedule;
        const productWeight = productWeightAt(schedule, seconds);
        const breakAllowance = smoothProgressBetween(schedule.bondsBreakStartSeconds - BREAK_LEAD_SECONDS, schedule.bondsBreakStartSeconds, seconds);
        const previewAllowance = smoothProgressBetween(schedule.collisionStartSeconds, schedule.collisionEndSeconds, seconds);

        for (const measured of this.breaking) {
            showOrder(measured.bond, Math.min(1 - breakAllowance * (1 - this.orderOf(measured, seconds, 1)), 1 - productWeight), MIN_SOLID_ORDER);
        }

        for (const measured of this.measuredPreviews) {
            showOrder(measured.bond, Math.max(previewAllowance * this.orderOf(measured, seconds, 0), productWeight), NEVER_SOLID);
        }

        this.survivingBeforeSwap.forEach(showStretch);
    }

    showAfterSwap(seconds: number): void {
        const productWeight = productWeightAt(this.recording.schedule, seconds);

        for (const measured of this.forming) {
            showOrder(measured.bond, Math.max(this.orderOf(measured, seconds, 0), productWeight), MIN_SOLID_ORDER);
        }

        for (const measured of this.measuredEchoes) {
            showOrder(measured.bond, Math.min(this.orderOf(measured, seconds, 1), 1 - productWeight), NEVER_SOLID);
        }

        this.survivingAfterSwap.forEach(showStretch);
    }

    private orderOf(measured: MeasuredBond, seconds: number, orderOutsideRecording: number): number {
        if (measured.fromRecordingIndex === OUTSIDE_RECORDING || measured.toRecordingIndex === OUTSIDE_RECORDING) {
            return orderOutsideRecording;
        }

        let lengthSum = 0;

        for (const offset of SAMPLE_OFFSETS_SECONDS) {
            recordedPositionAt(this.recording, measured.fromRecordingIndex, seconds + offset, this.fromPosition);
            recordedPositionAt(this.recording, measured.toRecordingIndex, seconds + offset, this.toPosition);
            lengthSum += this.fromPosition.distanceTo(this.toPosition);
        }

        return Math.min(Math.exp((measured.restLength - lengthSum / SAMPLE_OFFSETS_SECONDS.length) / PAULING_LENGTH), 1);
    }
}

function mirrorsOf(bond: PlacedBond, partnerByAtom: ReadonlyMap<PlacedAtom, PlacedAtom>): MirroredBond[] {
    const from = partnerByAtom.get(bond.from);
    const to = partnerByAtom.get(bond.to);

    return COVALENT_KINDS.has(bond.kind) && from && to
        ? [{ mirror: { from, to, kind: bond.kind, sidePoint: new Vector3(), strength: 0, partial: false }, original: bond }]
        : [];
}

function measuredOf(shownBond: PlacedBond, restingBond: PlacedBond, indexByAtom: RecordingIndexByAtom): MeasuredBond {
    return {
        bond: shownBond,
        fromRecordingIndex: indexByAtom.get(restingBond.from) ?? OUTSIDE_RECORDING,
        toRecordingIndex: indexByAtom.get(restingBond.to) ?? OUTSIDE_RECORDING,
        restLength: restingBond.from.position.distanceTo(restingBond.to.position)
    };
}

function showOrder(bond: PlacedBond, order: number, minSolidOrder: number): void {
    bond.partial = order > MAX_HIDDEN_ORDER && order < minSolidOrder;

    if (order <= MAX_HIDDEN_ORDER) {
        bond.strength = 0;
    } else {
        bond.strength = bond.partial ? PARTIAL_STRENGTH.floor + PARTIAL_STRENGTH.perOrder * order : 1;
    }
}

function showStretch(measured: MeasuredBond): void {
    const length = Math.max(measured.bond.from.position.distanceTo(measured.bond.to.position), MIN_BOND_LENGTH);
    
    measured.bond.partial = false;
    measured.bond.strength = Math.min(Math.max(Math.sqrt(measured.restLength / length), SURVIVING_STRENGTH.least), SURVIVING_STRENGTH.most);
}