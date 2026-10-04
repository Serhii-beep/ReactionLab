import { Quaternion, Vector3 } from "three";
import { MolecularSystem, Molecule, SpringPull, TIME_UNITS_PER_SECOND } from "./molecular-system";
import { DynamicsSchedule } from "./dynamics-schedule";
import { FLOOR_CLEARANCE } from "../../scene/bench-layout";
import { easeInOutCubic, easeInOutCubicRateOf, progressBetween, smoothProgressBetween } from "../easing";
import { bestFitRotation } from "../../core/best-fit-rotation";

interface LandingProduct {
    readonly molecule: Molecule;
    readonly restCenter: Vector3;
    readonly restShape: readonly Vector3[];
    readonly currentShape: Vector3[];
    readonly hopHeight: number;
}

interface PoseTiming {
    readonly weight: number;
    readonly turn: number;
    readonly turnRate: number;
    readonly hop: number;
}

const SETTLE = { startSeconds: 0, fullSeconds: 0.85, rate: 4 };
const POSE = { startSeconds: 0.7, fullSeconds: 1.4, turnSeconds: 1.0, rate: 9 };
const CRITICAL_DAMPING = 2;
const IDENTITY = new Quaternion();

const FULL_HOP_TURN_RADIANS = Math.PI / 2;

export class LandingGuides {
    private readonly products: readonly LandingProduct[];
    private readonly moleculeCenter = new Vector3();
    private readonly moleculeDrift = new Vector3();
    private readonly landingSpot = new Vector3();
    private readonly acceleration = new Vector3();
    private readonly poseAnchor = new Vector3();
    private readonly anchorVelocity = new Vector3();
    private readonly homeward = new Vector3();
    private readonly goalPosition = new Vector3();
    private readonly currentTurn = new Quaternion();
    private readonly poseTurn = new Quaternion();

    constructor(
        private readonly system: MolecularSystem,
        molecules: readonly Molecule[],
        productRest: Float64Array,
        private readonly schedule: DynamicsSchedule) {
        this.products = molecules.map((molecule) => landingProductOf(molecule, system, productRest));
    }

    accumulate(seconds: number, airFrictionPerSecond: number): void {
        const sinceRelease = seconds - this.schedule.releaseSeconds;
        const settleWeight = smoothProgressBetween(SETTLE.startSeconds, SETTLE.fullSeconds, sinceRelease);

        if (settleWeight <= 0) {
            return;
        }

        const timing = poseTimingAt(sinceRelease);
        const spring: SpringPull = { omega: POSE.rate / TIME_UNITS_PER_SECOND, damping: CRITICAL_DAMPING, weight: timing.weight, drift: this.anchorVelocity, minPulledMass: 0 };
        const settleFriction = settleFrictionOf(settleWeight, airFrictionPerSecond);

        for (const product of this.products) {
            this.measure(product, timing.weight > 0 && timing.turn < 1);
            this.landingSpot.copy(product.restCenter).setY(product.restCenter.y + product.hopHeight * hopShareOf(this.currentTurn) * timing.hop);
            this.settle(product, settleWeight, settleFriction);

            if (timing.weight > 0) {
                this.pose(product, timing, spring);
            }
        }
    }

    private measure(product: LandingProduct, turning: boolean): void {
        const { atomIndices, mass } = product.molecule;

        this.system.massWeightedMeanOf(atomIndices, mass, this.system.positions, this.moleculeCenter);
        this.system.massWeightedMeanOf(atomIndices, mass, this.system.velocities, this.moleculeDrift);
        this.currentTurn.identity();

        if (turning && atomIndices.length > 1) {
            atomIndices.forEach((index, position) => this.system.offsetOf(index, this.moleculeCenter, product.currentShape[position]));
            bestFitRotation(product.restShape, product.currentShape, this.currentTurn);
        }
    }

    private settle(product: LandingProduct, weight: number, friction: number): void {
        const omega = SETTLE.rate / TIME_UNITS_PER_SECOND;

        this.acceleration.subVectors(this.landingSpot, this.moleculeCenter).multiplyScalar(weight * omega * omega)
            .addScaledVector(this.moleculeDrift, -friction);
        this.system.accelerate(product.molecule.atomIndices, this.acceleration, 1);
    }

    private pose(product: LandingProduct, timing: PoseTiming, spring: SpringPull): void {
        const { turn, turnRate } = timing;

        this.poseTurn.slerpQuaternions(this.currentTurn, IDENTITY, turn);
        this.poseAnchor.lerpVectors(this.moleculeCenter, this.landingSpot, turn);
        this.homeward.subVectors(this.landingSpot, this.moleculeCenter);
        this.anchorVelocity.copy(this.moleculeDrift).multiplyScalar(1 - turn).addScaledVector(this.homeward, turnRate);
        product.molecule.atomIndices.forEach((index, position) => {
            this.goalPosition.copy(product.restShape[position]).applyQuaternion(this.poseTurn).add(this.poseAnchor);
            this.system.pullToward(index, this.goalPosition, spring);
        });
    }
}

function settleFrictionOf(weight: number, airFrictionPerSecond: number): number {
    const omega = SETTLE.rate / TIME_UNITS_PER_SECOND;

    return Math.max(CRITICAL_DAMPING * Math.sqrt(weight) * omega - airFrictionPerSecond / TIME_UNITS_PER_SECOND, 0);
}

function poseTimingAt(sinceRelease: number): PoseTiming {
    const turnProgress = progressBetween(POSE.startSeconds, POSE.startSeconds + POSE.turnSeconds, sinceRelease);

    return {
        weight: smoothProgressBetween(POSE.startSeconds, POSE.fullSeconds, sinceRelease),
        turn: easeInOutCubic(turnProgress),
        turnRate: turnProgress < 1 ? easeInOutCubicRateOf(turnProgress) / (POSE.turnSeconds * TIME_UNITS_PER_SECOND) : 0,
        hop: Math.sin(Math.PI * turnProgress)
    };
}

function hopShareOf(turn: Quaternion): number {
    const angle = 2 * Math.acos(Math.min(Math.abs(turn.w), 1));

    return Math.min(angle / FULL_HOP_TURN_RADIANS, 1);
}

function landingProductOf(molecule: Molecule, system: MolecularSystem, productRest: Float64Array): LandingProduct {
    const restPoints = molecule.atomIndices.map((index) => new Vector3(productRest[index * 3], productRest[index * 3 + 1], productRest[index * 3 + 2]));
    const restCenter = system.massWeightedMeanOf(molecule.atomIndices, molecule.mass, productRest, new Vector3());
    const restShape = restPoints.map((point) => point.clone().sub(restCenter));
    const reach = molecule.atomIndices.reduce((farthest, index, position) => Math.max(farthest, restShape[position].length() + system.atoms[index].radius), 0);

    return {
        molecule,
        restCenter,
        restShape,
        currentShape: restPoints.map(() => new Vector3()),
        hopHeight: Math.max(reach + FLOOR_CLEARANCE - restCenter.y, 0)
    };
}