import { Box3, Sphere, Vector3 } from "three";
import { BenchLayout } from "../scene/bench-layout";
import { easeInOutCubic, easeOutCubic, easeOutPower, progressBetween, progressWithin } from "./easing";
import { PhaseSpanSeconds, ReactionScript } from "./reaction-script";
import { atomsOfUnits, bondsOfUnits, CentersByUnitId, gatheredCentersOf, meetingPointOf, mergePosedPoints, pointsAtPose, poseUnits, StagedBench, StagedSet, stageTravellingUnits, TravelPlanFor, UnitPose, UnitTravelPlan } from "./unit-gathering";
import { AtomMorph } from "./atom-morph";
import { BondChanges, classifyBonds, indexBondsByEnds, setStrength } from "./bond-continuity";
import { pairAtoms } from "./atom-pairing";
import { refinePairing } from "./pairing-refinement";

const GATHERED_REACTANTS: Readonly<UnitPose> = Object.freeze({ travelProgress: 1, turnFraction: 1, recoilAngstrom: 0 });
const GATHERED_PRODUCTS: Readonly<UnitPose> = Object.freeze({ travelProgress: 0, turnFraction: 1, recoilAngstrom: 0 });
const MAX_TUMBLE_RADIANS = Math.PI / 4;
const TUMBLE_VARIATION_FLOOR = 0.6;
const HASH_MULTIPLIER = 31;
const HASH_MODULUS = 1000003;
const VARIATION_STEPS = 100;

export class ReactionMotion {
    readonly bounds: Box3;

    private readonly reactantsSet: StagedSet;
    private readonly productsSet: StagedSet;
    private readonly morph: AtomMorph;
    private readonly bondChanges: BondChanges;
    private readonly framePoseScratch: UnitPose = { travelProgress: 0, turnFraction: 0, recoilAngstrom: 0 };

    constructor(private readonly script: ReactionScript, before: BenchLayout, after: BenchLayout) {
        const { phases, tuning } = script;
        const consumedIds = new Set([...before.sphereByUnitId.keys()].filter((unitId) => !after.sphereByUnitId.has(unitId)));
        const producedIds = new Set([...after.sphereByUnitId.keys()].filter((unitId) => !before.sphereByUnitId.has(unitId)));
        const meeting = meetingPointOf(before, [...consumedIds]);
        const approachTumble = Math.min(tuning.tumbleRadiansPerSecond * secondsOf(phases.approach), MAX_TUMBLE_RADIANS);
        const separationTumble = Math.min(tuning.tumbleRadiansPerSecond * secondsOf(phases.separation), MAX_TUMBLE_RADIANS);

        this.bounds = before.bounds.clone().union(after.bounds);
        this.reactantsSet = stageTravellingUnits(before, approachPlans(gatheredCentersOf(before, [...consumedIds], meeting), approachTumble));
        this.productsSet = stageTravellingUnits(after, releasePlans(gatheredCentersOf(after, [...producedIds], meeting), before, separationTumble));

        const gathered = mergePosedPoints(pointsAtPose(this.reactantsSet, GATHERED_REACTANTS), pointsAtPose(this.productsSet, GATHERED_PRODUCTS));
        const reactantBonds = bondsOfUnits(this.reactantsSet, consumedIds);
        const productBonds = bondsOfUnits(this.productsSet, producedIds);
        const productBondByEnds = indexBondsByEnds(productBonds);
        const nearestPairs = pairAtoms(atomsOfUnits(this.reactantsSet, consumedIds), atomsOfUnits(this.productsSet, producedIds), gathered);
        const atomPairs = refinePairing(nearestPairs, reactantBonds, productBondByEnds, gathered);

        this.bondChanges = classifyBonds(reactantBonds, productBonds, productBondByEnds, atomPairs);
        this.morph = new AtomMorph(atomPairs, this.bondChanges.surviving, { gathered, meeting, tuning });
    }

    frameAt(seconds: number): StagedBench {
        const { phases, tuning } = this.script;

        if (seconds < phases.transitionState.end) {
            const approach = easeInOutCubic(progressWithin(phases.approach, seconds));

            this.framePoseScratch.travelProgress = approach;
            this.framePoseScratch.turnFraction = approach;
            this.framePoseScratch.recoilAngstrom = seconds < phases.collision.end
                ? tuning.recoilAngstrom * Math.sin(Math.PI * progressWithin(phases.collision, seconds))
                : 0;
            poseUnits(this.reactantsSet, this.framePoseScratch);

            if (seconds >= phases.bondsBreak.start) {
                this.morph.placeReactants(this.rearrangementAt(seconds), this.jitterEnvelopeAt(seconds), seconds);
            }

            setStrength(this.bondChanges.breaking, 1 - easeOutCubic(progressWithin(phases.bondsBreak, seconds)));

            return this.reactantsSet;
        }

        const release = easeOutPower(progressWithin(phases.separation, seconds), tuning.releaseExponent);

        this.framePoseScratch.travelProgress = release;
        this.framePoseScratch.turnFraction = 1 - release;
        this.framePoseScratch.recoilAngstrom = 0;
        poseUnits(this.productsSet, this.framePoseScratch);

        if (seconds < phases.bondsForm.end) {
            this.morph.placeProducts(this.rearrangementAt(seconds), this.jitterEnvelopeAt(seconds), seconds);
        }

        setStrength(this.bondChanges.forming, easeOutCubic(progressWithin(phases.bondsForm, seconds)));

        return this.productsSet;
    }

    private rearrangementAt(seconds: number): number {
        const { phases } = this.script;

        return easeInOutCubic(progressBetween(phases.bondsBreak.start, phases.bondsForm.end, seconds));
    }

    private jitterEnvelopeAt(seconds: number): number {
        const { phases } = this.script;

        return Math.sin(Math.PI * progressBetween(phases.bondsBreak.start, phases.bondsForm.end, seconds));
    }
}

function secondsOf(span: PhaseSpanSeconds): number {
    return span.end - span.start;
}

function approachPlans(gatheredCenterByUnitId: CentersByUnitId, referenceTumbleRadians: number): TravelPlanFor {
    return (unitId, sphere) => {
        const gatheredCenter = gatheredCenterByUnitId.get(unitId);

        if (!gatheredCenter) {
            return null;
        }

        return { fromShift: new Vector3(), toShift: gatheredCenter.clone().sub(sphere.center), tumbleRadians: tumbleOf(unitId, referenceTumbleRadians) };
    };
}

function releasePlans(gatheredCenterByUnitId: CentersByUnitId, before: BenchLayout, referenceTumbleRadians: number): TravelPlanFor {
    return (unitId, sphere) => {
        const gatheredCenter = gatheredCenterByUnitId.get(unitId);

        if (gatheredCenter) {
            return { fromShift: gatheredCenter.clone().sub(sphere.center), toShift: new Vector3(), tumbleRadians: tumbleOf(unitId, referenceTumbleRadians) };
        }

        return spectatorPlan(before.sphereByUnitId.get(unitId), sphere);
    };
}

function spectatorPlan(restingBefore: Sphere | undefined, sphere: Sphere): UnitTravelPlan | null {
    return restingBefore ? { fromShift: restingBefore.center.clone().sub(sphere.center), toShift: new Vector3(), tumbleRadians: 0 } : null;
}

function tumbleOf(unitId: string, referenceTumbleRadians: number): number {
    let hash = 0;

    for (const character of unitId) {
        hash = (hash * HASH_MULTIPLIER + character.charCodeAt(0)) % HASH_MODULUS;
    }

    const sign = hash % 2 === 0 ? 1 : -1;
    const variation = TUMBLE_VARIATION_FLOOR + (1 - TUMBLE_VARIATION_FLOOR) * ((Math.floor(hash / 2) % VARIATION_STEPS) / VARIATION_STEPS);

    return sign * variation * referenceTumbleRadians;
}