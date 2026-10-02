import { Quaternion, Vector3 } from "three";
import { MolecularSystem, Molecule, SpringPull, TIME_UNITS_PER_SECOND } from "./molecular-system";
import { DynamicsSchedule } from "./dynamics-schedule";
import { formedArrangementOf } from "./formed-arrangement";
import { easeInOutCubic, progressBetween, smoothProgressBetween } from "../easing";
import { bestFitRotation } from "../../core/best-fit-rotation";

interface FirstContact {
    readonly startShape: readonly Vector3[];
    readonly formedShape: readonly Vector3[];
    readonly holdPoint: Vector3;
}

const SHAPE_RATE = 7;
const SHAPE_DAMPING = 0.6;
const HOLD_RATE = 3;
const CRITICAL_DAMPING = 2;

export class ActivationShaping {
    private readonly clusterAtomIndices: readonly number[];
    private readonly clusterMass: number;
    private readonly currentShape: Vector3[];
    private readonly desiredShape: Vector3[];
    private readonly clusterTurn = new Quaternion();
    private readonly clusterCenter = new Vector3();
    private readonly clusterDrift = new Vector3();
    private readonly goalPosition = new Vector3();
    private readonly holdAcceleration = new Vector3();
    private firstContact: FirstContact | null = null;

    constructor(
        private readonly system: MolecularSystem,
        private readonly products: readonly Molecule[],
        private readonly productRest: Float64Array,
        private readonly schedule: DynamicsSchedule
    ) {
        this.clusterAtomIndices = system.atoms.map((_, index) => index);
        this.clusterMass = system.atoms.reduce((sum, atom) => sum + atom.mass, 0);
        this.currentShape = system.atoms.map(() => new Vector3());
        this.desiredShape = system.atoms.map(() => new Vector3());
    }

    accumulate(seconds: number, productWeight: number): void {
        const { collisionStartSeconds, bondsBreakStartSeconds, switchStartSeconds } = this.schedule;
        const weight = smoothProgressBetween(collisionStartSeconds, bondsBreakStartSeconds, seconds) * (1 - productWeight);

        if (weight <= 0) {
            return;
        }

        this.measure();

        const firstContact = this.firstContact ?? this.captureFirstContact();
        const progress = easeInOutCubic(progressBetween(collisionStartSeconds, switchStartSeconds, seconds));

        this.desiredShape.forEach((offset, index) => offset.lerpVectors(firstContact.startShape[index], firstContact.formedShape[index], progress));
        bestFitRotation(this.desiredShape, this.currentShape, this.clusterTurn);
        this.pullIntoShape(weight);
        this.holdInPlace(firstContact.holdPoint, weight);
    }

    private measure(): void {
        this.system.massWeightedMeanOf(this.clusterAtomIndices, this.clusterMass, this.system.positions, this.clusterCenter);
        this.system.massWeightedMeanOf(this.clusterAtomIndices, this.clusterMass, this.system.velocities, this.clusterDrift);
        this.currentShape.forEach((offset, index) => this.system.offsetOf(index, this.clusterCenter, offset));
    }

    private captureFirstContact(): FirstContact {
        const firstContact: FirstContact = {
            startShape: this.currentShape.map((offset) => offset.clone()),
            formedShape: formedArrangementOf(this.currentShape, this.products, this.productRest, this.system.atoms),
            holdPoint: this.clusterCenter.clone()
        };

        this.firstContact = firstContact;

        return firstContact;
    }

    private pullIntoShape(weight: number): void {
        const spring: SpringPull = { omega: SHAPE_RATE / TIME_UNITS_PER_SECOND, damping: SHAPE_DAMPING, weight, drift: this.clusterDrift };

        this.desiredShape.forEach((offset, index) => {
            this.goalPosition.copy(offset).applyQuaternion(this.clusterTurn).add(this.clusterCenter);
            this.system.pullToward(index, this.goalPosition, spring);
        });
    }

    private holdInPlace(holdPoint: Vector3, weight: number): void {
        const omega = HOLD_RATE / TIME_UNITS_PER_SECOND;

        this.holdAcceleration.subVectors(holdPoint, this.clusterCenter).multiplyScalar(omega * omega)
            .addScaledVector(this.clusterDrift, -CRITICAL_DAMPING * omega);
        this.system.accelerate(this.clusterAtomIndices, this.holdAcceleration, weight);
    }
}