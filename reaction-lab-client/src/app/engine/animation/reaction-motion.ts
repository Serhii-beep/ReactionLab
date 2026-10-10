import { Box3, Sphere, Vector3 } from "three";
import { BenchLayout, PlacedBond } from "../scene/bench-layout";
import { easeInOutCubic, easeInToGlide, progressWithin, smoothProgressBetween } from "./easing";
import { ReactionEnergetics, ReactionScript } from "./reaction-script";
import { BondChanges, classifyBonds, indexBondsByEnds } from "./bond-continuity";
import { DynamicsRecording, recordedReachAt } from "./dynamics/dynamics-recording";
import { atomsOfUnits, bondsOfUnits, mergePosedPoints, pointsAtPose, poseTravels, StagedBench, StagedSet, stageTravelingUnits, TravelPlanFor, UnitTravel } from "./unit-staging";
import { dynamicsScheduleOf, swapSecondsOf } from "./dynamics/dynamics-schedule";
import { RecordedAtomLooks, RecordedUnits } from "./recorded-units";
import { buildDynamicsInput, DynamicsSources, RecordingIndexByAtom, recordingIndexByAtom } from "./dynamics-input-builder";
import { boundsWithGathering, CentersByUnitId, gatheredSphereOf, gatheringOf, slotCentersAround } from "./gathering";
import { AtomPair, pairAtoms } from "./atom-pairing";
import { refinePairing } from "./pairing-refinement";
import { bakeReactionDynamics } from "./dynamics/reaction-dynamics";
import { RunBonds } from "./run-bonds";
import { CameraCues } from "../cinematography/camera-cues";
import { EnergyLedger, shownEnthalpyOf } from "./energy-ledger";
import { GasRun } from "../gas/gas-run";
import { buildGasRun } from "./gas-run-builder";
import { ReactionTrace } from "./reaction-trace";
import { buildReactionTrace } from "./reaction-trace-builder";
import { CrystalGrowth } from "./crystal-growth";
import { RadiusAt } from "./radius-blend";

interface RunFrameSources {
    readonly reactantsSet: StagedSet;
    readonly productsSet: StagedSet;
    readonly runBonds: RunBonds;
    readonly growth: CrystalGrowth | null;
    readonly before: BenchLayout;
    readonly after: BenchLayout;
    readonly consumedIds: ReadonlySet<string>;
    readonly producedIds: ReadonlySet<string>;
}

interface RunFrames {
    readonly reactants: StagedBench;
    readonly products: StagedBench;
    readonly landingLatticeBonds: readonly PlacedBond[];
}

const LANDING_BLEND_SECONDS = 0.3;
const RECORDING_BY_SCRIPT = new WeakMap<ReactionScript, DynamicsRecording>();

export class ReactionMotion {
    readonly bounds: Box3;
    readonly cameraCues: CameraCues;
    readonly trace: ReactionTrace;
    readonly gasRun: GasRun | null;

    private readonly swapSeconds: number;
    private readonly reactantsSet: StagedSet;
    private readonly productsSet: StagedSet;
    private readonly runBonds: RunBonds;
    private readonly reactantsFrame: StagedBench;
    private readonly productsFrame: StagedBench;
    private readonly recording: DynamicsRecording;
    private readonly recordedReactants: RecordedUnits;
    private readonly recordedProducts: RecordedUnits;
    private readonly reactantTravels: readonly UnitTravel[];
    private readonly productTravels: readonly UnitTravel[];
    private readonly landingLatticeBonds: readonly PlacedBond[];
    private readonly growth: CrystalGrowth | null;

    constructor(private readonly script: ReactionScript, before: BenchLayout, after: BenchLayout) {
        const consumedIds = unitIdsOnlyIn(before, after);
        const producedIds = unitIdsOnlyIn(after, before);
        const gathering = gatheringOf(before, [...consumedIds]);
        const productCenters = slotCentersAround(gathering.meeting, after, [...producedIds]);

        const schedule = dynamicsScheduleOf(script.phases, script.durationSeconds);
        this.swapSeconds = swapSecondsOf(schedule);
        this.bounds = boundsWithGathering(before, after, gathering);
        this.reactantsSet = stageTravelingUnits(before, approachPlans(gathering.centerByUnitId));
        this.productsSet = stageTravelingUnits(after, releasePlans(productCenters, before));

        const gathered = mergePosedPoints(pointsAtPose(this.reactantsSet, 1), pointsAtPose(this.productsSet, 0));
        const reactantBonds = bondsOfUnits(this.reactantsSet, consumedIds);
        const productBonds = bondsOfUnits(this.productsSet, producedIds);
        const productBondByEnds = indexBondsByEnds(productBonds);
        const nearestPairs = pairAtoms(atomsOfUnits(this.reactantsSet, consumedIds), atomsOfUnits(this.productsSet, producedIds), gathered);
        const atomPairs = refinePairing(nearestPairs, reactantBonds, productBondByEnds, gathered);
        const reactantIndexByAtom = recordingIndexByAtom(atomPairs.map((pair) => pair.reactant));
        const productIndexByAtom = recordingIndexByAtom(atomPairs.map((pair) => pair.product));

        const bondChanges = classifyBonds(reactantBonds, productBonds, productBondByEnds, atomPairs);
        const dynamicsSources: DynamicsSources = {
            script,
            schedule,
            atomPairs,
            reactantIndexByAtom,
            productIndexByAtom,
            reactantBonds,
            productBonds,
            gathering,
            restSphereByUnitId: before.sphereByUnitId
        };
        const ledger = energyLedgerOf(dynamicsSources, bondChanges);

        const gatheredSphere = gatheredSphereOf(before, gathering);

        this.recording = recordingFor(dynamicsSources);
        this.cameraCues = cameraCuesOf(gatheredSphere, after.bounds, this.recording, atomPairs, script.energetics);
        this.trace = buildReactionTrace(dynamicsSources, bondChanges, ledger, this.recording, { start: script.durationSeconds - LANDING_BLEND_SECONDS, end: script.durationSeconds });
        this.gasRun = buildGasRun(dynamicsSources, this.trace);
        const atomLooks = atomLooksOf(ledger, this.trace.radiusAt);

        this.recordedReactants = new RecordedUnits(this.reactantsSet, reactantIndexByAtom, this.recording, { restSeconds: 0, towardSeconds: this.swapSeconds }, atomLooks);
        this.recordedProducts = new RecordedUnits(this.productsSet, productIndexByAtom, this.recording, { restSeconds: script.durationSeconds, towardSeconds: this.swapSeconds }, atomLooks);
        this.reactantTravels = this.reactantsSet.travels.filter((travel) => !this.recordedReactants.unitIds.has(travel.unitId));
        this.productTravels = this.productsSet.travels.filter((travel) => !this.recordedProducts.unitIds.has(travel.unitId));
        this.runBonds = new RunBonds({ bondChanges, atomPairs, reactantIndexByAtom, productIndexByAtom, recording: this.recording });
        this.growth = script.precipitate === null ? null : new CrystalGrowth({ plan: script.precipitate, after, unitsAfter: script.unitsAfter, solution: gatheredSphere, schedule, randomSeed: script.randomSeed });

        const frames = runFramesOf({ reactantsSet: this.reactantsSet, productsSet: this.productsSet, runBonds: this.runBonds, growth: this.growth, before, after, consumedIds, producedIds });

        this.reactantsFrame = frames.reactants;
        this.productsFrame = frames.products;
        this.landingLatticeBonds = frames.landingLatticeBonds;
    }

    frameAt(seconds: number): StagedBench {
        const { phases, durationSeconds } = this.script;

        this.growth?.place(seconds);

        if (seconds < this.swapSeconds) {
            this.recordedReactants.place(seconds, 0);
            poseTravels(this.reactantTravels, easeInToGlide(progressWithin(phases.approach, seconds)));
            this.runBonds.showBeforeSwap(seconds);

            return this.reactantsFrame;
        }

        const landing = smoothProgressBetween(durationSeconds - LANDING_BLEND_SECONDS, durationSeconds, seconds);

        this.recordedProducts.place(seconds, landing);
        poseTravels(this.productTravels, easeInOutCubic(progressWithin(phases.separation, seconds)));
        this.runBonds.showAfterSwap(seconds);

        for (const bond of this.landingLatticeBonds) {
            bond.strength = landing;
        }

        return this.productsFrame;
    }
}

function recordingFor(sources: DynamicsSources): DynamicsRecording {
    const cached = RECORDING_BY_SCRIPT.get(sources.script);

    if (cached) { 
        return cached;
    }

    const recording = bakeReactionDynamics(buildDynamicsInput(sources));

    RECORDING_BY_SCRIPT.set(sources.script, recording);

    return recording;
}

function atomLooksOf(ledger: EnergyLedger, radiusAt: RadiusAt): RecordedAtomLooks {
    return {
        temperatureKelvinAt: (recordingIndex, seconds) => ledger.temperatureKelvinAt(recordingIndex, seconds),
        radiusAt
    };
}

function energyLedgerOf(sources: DynamicsSources, bondChanges: BondChanges): EnergyLedger {
    return new EnergyLedger({
        schedule: sources.schedule,
        energetics: sources.script.energetics,
        breakingAtomIndices: recordingIndicesOf(bondChanges.breaking, sources.reactantIndexByAtom),
        formingAtomIndices: recordingIndicesOf(bondChanges.forming, sources.productIndexByAtom),
        productUnitIds: sources.atomPairs.map((pair) => pair.product.unitId)
    });
}

function recordingIndicesOf(bonds: readonly PlacedBond[], indexByAtom: RecordingIndexByAtom): Set<number> {
    const indices = new Set<number>();

    for (const { from, to } of bonds) {
        for (const atom of [from, to]) {
            const index = indexByAtom.get(atom);

            if (index !== undefined) {
                indices.add(index);
            }
        }
    }

    return indices;
}

function cameraCuesOf(gathered: Sphere, finalBounds: Box3, recording: DynamicsRecording, atomPairs: readonly AtomPair[], energetics: ReactionEnergetics): CameraCues {
    const atomRadii = atomPairs.map((pair) => pair.reactant.radius);

    return {
        gathered,
        finalBounds,
        releaseSeconds: recording.schedule.releaseSeconds,
        shownEnthalpyKilojoulesPerMole: shownEnthalpyOf(energetics) ?? 0,
        reachAt: (seconds, center) => recordedReachAt(recording, atomRadii, seconds, center)
    };
}

function runFramesOf({ reactantsSet, productsSet, runBonds, growth, before, after, consumedIds, producedIds }: RunFrameSources): RunFrames {
    const standIns = { atoms: growth?.atoms ?? [], bonds: growth?.bonds ?? [], latticeBonds: growth?.latticeBonds ?? [] };

    return {
        reactants: {
            atoms: [...reactantsSet.atoms, ...standIns.atoms],
            bonds: [...reactantsSet.bonds, ...runBonds.previews, ...standIns.bonds],
            latticeBonds: latticeBondsOf(before, consumedIds, false),
            sphereByUnitId: reactantsSet.sphereByUnitId
        },
        products: {
            atoms: [...productsSet.atoms, ...standIns.atoms],
            bonds: [...productsSet.bonds, ...runBonds.echoes, ...standIns.bonds],
            latticeBonds: [...after.latticeBonds, ...standIns.latticeBonds],
            sphereByUnitId: productsSet.sphereByUnitId
        },
        landingLatticeBonds: latticeBondsOf(after, producedIds, true)
    };
}

function latticeBondsOf(layout: BenchLayout, unitIds: ReadonlySet<string>, touching: boolean): PlacedBond[] {
    return layout.latticeBonds.filter((bond) => (unitIds.has(bond.from.unitId) || unitIds.has(bond.to.unitId)) === touching);
}

function unitIdsOnlyIn(layout: BenchLayout, other: BenchLayout): Set<string> {
    return new Set([...layout.sphereByUnitId.keys()].filter((unitId) => !other.sphereByUnitId.has(unitId)));
}

function approachPlans(slotByUnitId: CentersByUnitId): TravelPlanFor {
    return (unitId, sphere) => {
        const slot = slotByUnitId.get(unitId);

        return slot ? { fromShift: new Vector3(), toShift: slot.clone().sub(sphere.center) } : null;
    };
}

function releasePlans(slotByUnitId: CentersByUnitId, before: BenchLayout): TravelPlanFor {
    return (unitId, sphere) => {
        const start = slotByUnitId.get(unitId) ?? before.sphereByUnitId.get(unitId)?.center;

        return start ? { fromShift: start.clone().sub(sphere.center), toShift: new Vector3() } : null;
    };
}
