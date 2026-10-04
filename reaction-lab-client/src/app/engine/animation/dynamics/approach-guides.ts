import { Vector3 } from "three";
import { MolecularSystem, Molecule, TIME_UNITS_PER_SECOND } from "./molecular-system";
import { approachWeightAt, DynamicsSchedule } from "./dynamics-schedule";
import { easeInToGlide, glideRateOf, progressBetween } from "../easing";

export interface ApproachArc {
    readonly from: Vector3;
    readonly lift: Vector3;
    readonly entry: Vector3;
    readonly to: Vector3;
}

interface ApproachPath {
    readonly molecule: Molecule;
    readonly arc: ApproachArc;
    readonly centerOffset: Vector3;
}

const LIFT = { shareOfRise: 0.6, extraAngstrom: 0.4 };
const ENTRY_REACH = 0.6;

const APPROACH_RATE = 9;

export function approachArcOf(from: Vector3, to: Vector3, meeting: Vector3): ApproachArc {
    return {
        from: from.clone(),
        lift: from.clone().setY(from.y + LIFT.shareOfRise * (to.y - from.y) + LIFT.extraAngstrom),
        entry: to.clone().sub(meeting).multiplyScalar(ENTRY_REACH).add(to),
        to: to.clone()
    };
}

export class ApproachGuides {
    private readonly paths: readonly ApproachPath[];
    private readonly moleculeCenter = new Vector3();
    private readonly moleculeDrift = new Vector3();
    private readonly goalPosition = new Vector3();
    private readonly goalVelocity = new Vector3();
    private readonly acceleration = new Vector3();

    constructor(
        private readonly system: MolecularSystem,
        molecules: readonly Molecule[],
        arcByUnitId: ReadonlyMap<string, ApproachArc>,
        private readonly schedule: DynamicsSchedule
    ) {
        this.paths = molecules.flatMap((molecule) => pathOf(system, molecule, arcByUnitId.get(molecule.unitId)));
    }

    accumulate(seconds: number): void {
        const weight = approachWeightAt(this.schedule, seconds);

        if (weight <= 0) {
            return;
        }

        const { approachEndSeconds } = this.schedule;
        const progress = progressBetween(0, approachEndSeconds, seconds);
        const travel = easeInToGlide(progress);
        const travelRate = progress < 1 ? glideRateOf(progress) / (approachEndSeconds * TIME_UNITS_PER_SECOND) : 0;
        const omega = APPROACH_RATE / TIME_UNITS_PER_SECOND;

        for (const { molecule, arc, centerOffset } of this.paths) {
            this.system.massWeightedMeanOf(molecule.atomIndices, molecule.mass, this.system.positions, this.moleculeCenter);
            this.system.massWeightedMeanOf(molecule.atomIndices, molecule.mass, this.system.velocities, this.moleculeDrift);
            pointOnArc(arc, travel, this.goalPosition).add(centerOffset);
            slopeOnArc(arc, travel, this.goalVelocity).multiplyScalar(travelRate).sub(this.moleculeDrift);
            this.acceleration.subVectors(this.goalPosition, this.moleculeCenter).multiplyScalar(omega * omega)
                .addScaledVector(this.goalVelocity, 2 * omega);
            this.system.accelerate(molecule.atomIndices, this.acceleration, weight);
        }
    }
}

function pathOf(system: MolecularSystem, molecule: Molecule, arc: ApproachArc | undefined): ApproachPath[] {
    if (arc === undefined) {
        return [];
    }

    const center = system.massWeightedMeanOf(molecule.atomIndices, molecule.mass, system.positions, new Vector3());

    return [{ molecule, arc, centerOffset: center.sub(arc.from) }];
}

function pointOnArc(arc: ApproachArc, travel: number, target: Vector3): Vector3 {
    const remaining = 1 - travel;

    return target.copy(arc.from).multiplyScalar(remaining * remaining * remaining)
        .addScaledVector(arc.lift, 3 * remaining * remaining * travel)
        .addScaledVector(arc.entry, 3 * remaining * travel * travel)
        .addScaledVector(arc.to, travel * travel * travel);
}

function slopeOnArc(arc: ApproachArc, travel: number, target: Vector3): Vector3 {
    const remaining = 1 - travel;

    return target.copy(arc.from).multiplyScalar(-3 * remaining * remaining)
        .addScaledVector(arc.lift, 3 * remaining * remaining - 6 * remaining * travel)
        .addScaledVector(arc.entry, 6 * remaining * travel - 3 * travel * travel)
        .addScaledVector(arc.to, 3 * travel * travel);
}