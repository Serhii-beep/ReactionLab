import { Box3, Vector3 } from "three";
import { BenchLayout } from "../scene/bench-layout";
import { easeInOutCubic, easeInToGlide, progressWithin, smoothProgressBetween } from "./easing";
import { ReactionScript } from "./reaction-script";
import { BondChanges, classifyBonds, indexBondsByEnds, setStrength } from "./bond-continuity";
import { EmissionAnchors } from "../particles/emission-plan";
import { DynamicsRecording } from "./dynamics/dynamics-recording";
import { atomsOfUnits, bondsOfUnits, mergePosedPoints, pointsAtPose, poseTravels, StagedBench, StagedSet, stageTravelingUnits, TravelPlanFor, UnitTravel } from "./unit-staging";
import { DynamicsSchedule, dynamicsScheduleOf, swapSecondsOf } from "./dynamics/dynamics-schedule";
import { RecordedUnits } from "./recorded-units";
import { buildDynamicsInput, DynamicsSources, recordingIndexByAtom } from "./dynamics-input-builder";
import { boundsWithGathering, CentersByUnitId, gatheringOf, slotCentersAround } from "./gathering";
import { pairAtoms } from "./atom-pairing";
import { refinePairing } from "./pairing-refinement";
import { bakeReactionDynamics } from "./dynamics/reaction-dynamics";

const LANDING_BLEND_SECONDS = 0.3;
const RECORDING_BY_SCRIPT = new WeakMap<ReactionScript, DynamicsRecording>();

export class ReactionMotion {
    readonly bounds: Box3;
    readonly emissionAnchors: EmissionAnchors;

    private readonly schedule: DynamicsSchedule;
    private readonly swapSeconds: number;
    private readonly reactantsSet: StagedSet;
    private readonly productsSet: StagedSet;
    private readonly bondChanges: BondChanges;
    private readonly recording: DynamicsRecording;
    private readonly recordedReactants: RecordedUnits;
    private readonly recordedProducts: RecordedUnits;
    private readonly reactantTravels: readonly UnitTravel[];
    private readonly productTravels: readonly UnitTravel[];

    constructor(private readonly script: ReactionScript, before: BenchLayout, after: BenchLayout) {
        const consumedIds = unitIdsOnlyIn(before, after);
        const producedIds = unitIdsOnlyIn(after, before);
        const gathering = gatheringOf(before, [...consumedIds]);
        const productCenters = slotCentersAround(gathering.meeting, after, [...producedIds]);

        this.schedule = dynamicsScheduleOf(script.phases, script.durationSeconds);
        this.swapSeconds = swapSecondsOf(this.schedule);
        this.bounds = boundsWithGathering(before, after, gathering);
        this.emissionAnchors = {
            meeting: gathering.meeting.clone(),
            products: [...productCenters.values()],
            reactants: [...gathering.centerByUnitId.values()]
        };
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

        this.bondChanges = classifyBonds(reactantBonds, productBonds, productBondByEnds, atomPairs);
        this.recording = recordingFor({
            script,
            schedule: this.schedule,
            atomPairs,
            reactantIndexByAtom,
            productIndexByAtom,
            reactantBonds,
            productBonds,
            gathering,
            restSphereByUnitId: before.sphereByUnitId
        });
        this.recordedReactants = new RecordedUnits(this.reactantsSet, reactantIndexByAtom);
        this.recordedProducts = new RecordedUnits(this.productsSet, productIndexByAtom);
        this.reactantTravels = this.reactantsSet.travels.filter((travel) => !this.recordedReactants.unitIds.has(travel.unitId));
        this.productTravels = this.productsSet.travels.filter((travel) => !this.recordedProducts.unitIds.has(travel.unitId));
    }

    frameAt(seconds: number): StagedBench {
        const { phases, durationSeconds } = this.script;

        if (seconds < this.swapSeconds) {
            this.recordedReactants.place(this.recording, seconds, 0);
            poseTravels(this.reactantTravels, easeInToGlide(progressWithin(phases.approach, seconds)));
            setStrength(this.bondChanges.breaking, 1 - smoothProgressBetween(this.schedule.switchStartSeconds, this.swapSeconds, seconds));

            return this.reactantsSet;
        }

        this.recordedProducts.place(this.recording, seconds, smoothProgressBetween(durationSeconds - LANDING_BLEND_SECONDS, durationSeconds, seconds));
        poseTravels(this.productTravels, easeInOutCubic(progressWithin(phases.separation, seconds)));
        setStrength(this.bondChanges.forming, smoothProgressBetween(this.swapSeconds, this.schedule.switchEndSeconds, seconds));

        return this.productsSet;
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