import { Box3, Sphere, Vector3 } from "three";
import { BenchLayout } from "../scene/bench-layout";
import { easeInOutCubic, easeOutCubic, progressBetween, progressWithin } from "./easing";
import { ReactionScript } from "./reaction-script";
import { atomsOfUnits, bondsOfUnits, CentersByUnitId, gatheredCentersOf, meetingPointOf, mergePosedPoints, pointsAtProgress, poseUnits, ShiftsFor, StagedBench, StagedSet, stageTravellingUnits, UnitShifts } from "./unit-gathering";
import { AtomMorph } from "./atom-morph";
import { BondChanges, classifyBonds, indexBondsByEnds, setStrength } from "./bond-continuity";
import { pairAtoms } from "./atom-pairing";
import { refinePairing } from "./pairing-refinement";

export class ReactionMotion {
    readonly bounds: Box3;

    private readonly reactantsSet: StagedSet;
    private readonly productsSet: StagedSet;
    private readonly morph: AtomMorph;
    private readonly bondChanges: BondChanges;

    constructor(private readonly script: ReactionScript, before: BenchLayout, after: BenchLayout) {
        const consumedIds = new Set([...before.sphereByUnitId.keys()].filter((unitId) => !after.sphereByUnitId.has(unitId)));
        const producedIds = new Set([...after.sphereByUnitId.keys()].filter((unitId) => !before.sphereByUnitId.has(unitId)));
        const meeting = meetingPointOf(before, [...consumedIds]);

        this.bounds = before.bounds.clone().union(after.bounds);
        this.reactantsSet = stageTravellingUnits(before, approachShifts(gatheredCentersOf(before, [...consumedIds], meeting)));
        this.productsSet = stageTravellingUnits(after, releaseShifts(gatheredCentersOf(after, [...producedIds], meeting), before));

        const gathered = mergePosedPoints(pointsAtProgress(this.reactantsSet, 1), pointsAtProgress(this.productsSet, 0));
        const reactantBonds = bondsOfUnits(this.reactantsSet, consumedIds);
        const productBonds = bondsOfUnits(this.productsSet, producedIds);
        const productBondByEnds = indexBondsByEnds(productBonds);
        const nearestPairs = pairAtoms(atomsOfUnits(this.reactantsSet, consumedIds), atomsOfUnits(this.productsSet, producedIds), gathered);
        const atomPairs = refinePairing(nearestPairs, reactantBonds, productBondByEnds, gathered);

        this.bondChanges = classifyBonds(reactantBonds, productBonds, productBondByEnds, atomPairs);
        this.morph = new AtomMorph(atomPairs, this.bondChanges.surviving, gathered);
    }

    frameAt(seconds: number): StagedBench {
        const { phases } = this.script;

        if (seconds < phases.transitionState.end) {
            poseUnits(this.reactantsSet, easeInOutCubic(progressWithin(phases.approach, seconds)));

            if (seconds >= phases.approach.end) {
                this.morph.placeReactants(this.rearrangementAt(seconds));
            }

            setStrength(this.bondChanges.breaking, 1 - easeOutCubic(progressWithin(phases.bondsBreak, seconds)));

            return this.reactantsSet;
        }

        poseUnits(this.productsSet, easeOutCubic(progressWithin(phases.separation, seconds)));

        if (seconds < phases.bondsForm.end) {
            this.morph.placeProducts(this.rearrangementAt(seconds));
        }

        setStrength(this.bondChanges.forming, easeOutCubic(progressWithin(phases.bondsForm, seconds)));

        return this.productsSet;
    }

    private rearrangementAt(seconds: number): number {
        const { phases } = this.script;

        return easeInOutCubic(progressBetween(phases.bondsBreak.start, phases.bondsForm.end, seconds));
    }
}

function approachShifts(gatheredCenterByUnitId: CentersByUnitId): ShiftsFor {
    return (unitId, sphere) => {
        const gatheredCenter = gatheredCenterByUnitId.get(unitId);

        return gatheredCenter ? { fromShift: new Vector3(), toShift: gatheredCenter.clone().sub(sphere.center) } : null;
    };
}

function releaseShifts(gatheredCenterByUnitId: CentersByUnitId, before: BenchLayout): ShiftsFor {
    return (unitId, sphere) => {
        const gatheredCenter = gatheredCenterByUnitId.get(unitId);

        if (gatheredCenter) {
            return { fromShift: gatheredCenter.clone().sub(sphere.center), toShift: new Vector3() };
        }

        return spectatorShifts(before.sphereByUnitId.get(unitId), sphere);
    };
}

function spectatorShifts(restingBefore: Sphere | undefined, sphere: Sphere): UnitShifts | null {
    return restingBefore ? { fromShift: restingBefore.center.clone().sub(sphere.center), toShift: new Vector3() } : null;
}