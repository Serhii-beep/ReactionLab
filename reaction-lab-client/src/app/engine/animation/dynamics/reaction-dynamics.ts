import { Vector3 } from "three";
import { smoothProgressBetween } from "../easing";
import { ReactionEnergetics } from "../reaction-script";
import { ActivationShaping } from "./activation-shaping";
import { ApproachArc, ApproachGuides } from "./approach-guides";
import { bondingPictureOf, dissociationSumOf, IndexedBond, withAntiBonding } from "./bonding-picture";
import { DynamicsRecording } from "./dynamics-recording";
import { DynamicsSchedule, productWeightAt } from "./dynamics-schedule";
import { AIR_DRAG_PER_SECOND, LaunchDraws, launchProducts, ReleaseBudget, releaseBudgetOf, ringDown } from "./energy-release";
import { InteratomicForces } from "./interatomic-forces";
import { LandingGuides } from "./landing-guides";
import { BathSetting, BENCH_TEMPERATURE_KELVIN, BOLTZMANN_KILOJOULES_PER_MOLE_KELVIN, LangevinBath } from "./langevin-bath";
import { MolecularSystem, Molecule, moleculesOf, SimulatedAtom, TIME_UNITS_PER_SECOND } from "./molecular-system";
import { seededRandom, standardNormalSource } from "../../core/seeded-random";

export interface DynamicsInput {
    readonly atoms: readonly SimulatedAtom[];
    readonly reactantRest: Float64Array;
    readonly productRest: Float64Array;
    readonly reactantBonds: readonly IndexedBond[];
    readonly productBonds: readonly IndexedBond[];
    readonly reactantUnitIds: readonly string[];
    readonly productUnitIds: readonly string[];
    readonly approachArcByUnitId: ReadonlyMap<string, ApproachArc>;
    readonly schedule: DynamicsSchedule;
    readonly energetics: ReactionEnergetics;
    readonly randomSeed: string;
}

const SUBSTEPS_PER_SECOND = 360;
const SUBSTEPS_PER_FRAME = 3;
const FRAMES_PER_SECOND = SUBSTEPS_PER_SECOND / SUBSTEPS_PER_FRAME;
const STEP_SECONDS = 1 / SUBSTEPS_PER_SECOND;
const STEP_TIME_UNITS = TIME_UNITS_PER_SECOND * STEP_SECONDS;
const REACTANT_TEMPERATURE_KELVIN = 1400;
const FRICTION_PER_SECOND = { beforeCollision: 2.5, reacting: 0.25, cooling: 2.6 };
const COOLING_AFTER_RELEASE = { startSeconds: 0.5, fullSeconds: 1.6 };
const NO_MOLECULES: readonly Molecule[] = [];

export function bakeReactionDynamics(input: DynamicsInput): DynamicsRecording {
    const simulation = new ReactionSimulation(input);
    const steps = input.atoms.length > 0 ? Math.ceil(input.schedule.durationSeconds * SUBSTEPS_PER_SECOND) : 0;
    const frames = [simulation.snapshot()];

    for (let step = 0; step < steps; step++) {
        simulation.advance(step * STEP_SECONDS);

        if ((step + 1) % SUBSTEPS_PER_FRAME === 0) {
            frames.push(simulation.snapshot());
        }
    }

    return { framesPerSecond: FRAMES_PER_SECOND, frames, schedule: input.schedule };
}

class ReactionSimulation {
    private readonly system: MolecularSystem;
    private readonly reactants: readonly Molecule[];
    private readonly products: readonly Molecule[];
    private readonly draws: LaunchDraws;
    private readonly releaseBudget: ReleaseBudget;
    private readonly interatomicForces: InteratomicForces;
    private readonly approachGuides: ApproachGuides;
    private readonly activationShaping: ActivationShaping;
    private readonly landingGuides: LandingGuides;
    private readonly bath: LangevinBath;
    private launched = false;
    private rungDown = false;

    constructor(private readonly input: DynamicsInput) {
        const { atoms, reactantRest, productRest, schedule } = input;
        const reactantPicture = bondingPictureOf(input.reactantBonds, atoms, reactantRest);
        const productPicture = bondingPictureOf(input.productBonds, atoms, productRest);
        const uniform = seededRandom(input.randomSeed);

        this.system = new MolecularSystem(atoms, reactantRest);
        this.reactants = moleculesOf(atoms, input.reactantUnitIds);
        this.products = moleculesOf(atoms, input.productUnitIds);
        this.draws = { uniform, standardNormal: standardNormalSource(uniform) };
        this.releaseBudget = releaseBudgetOf(input.energetics, dissociationSumOf(reactantPicture) - dissociationSumOf(productPicture), atoms.length);
        this.interatomicForces = new InteratomicForces(this.system, withAntiBonding(reactantPicture, productPicture), withAntiBonding(productPicture, reactantPicture));
        this.approachGuides = new ApproachGuides(this.system, this.reactants, input.approachArcByUnitId, schedule);
        this.activationShaping = new ActivationShaping(this.system, this.products, productRest, schedule);
        this.landingGuides = new LandingGuides(this.system, this.products, productRest, schedule);
        this.bath = new LangevinBath(this.system, this.draws.standardNormal);
        this.warm();
        this.computeForces(0);
    }

    snapshot(): Float32Array {
        return Float32Array.from(this.system.positions);
    }

    advance(seconds: number): void {
        this.releaseWhenDue(seconds);
        this.kick();
        this.drift();
        this.bath.thermalize(this.bathSettingAt(seconds), STEP_SECONDS);
        this.drift();
        this.computeForces(seconds + STEP_SECONDS);
        this.kick();
    }

    private releaseWhenDue(seconds: number): void {
        const { releaseSeconds, switchEndSeconds } = this.input.schedule;

        if (!this.launched && seconds >= releaseSeconds) {
            this.launched = true;
            launchProducts(this.system, this.products, this.input.productRest, this.releaseBudget, this.draws);
        }

        if (!this.rungDown && seconds >= switchEndSeconds) {
            this.rungDown = true;
            ringDown(this.system, this.products, this.releaseBudget);
        }
    }

    private bathSettingAt(seconds: number): BathSetting {
        const { schedule } = this.input;

        if (!this.launched) {
            const frictionPerSecond = seconds < schedule.collisionStartSeconds ? FRICTION_PER_SECOND.beforeCollision : FRICTION_PER_SECOND.reacting;

            return { frictionPerSecond, temperatureKelvin: REACTANT_TEMPERATURE_KELVIN, moleculesKeepingDrift: this.reactants };
        }

        const cooling = coolingAt(schedule, seconds);

        return {
            frictionPerSecond: airFrictionPerSecondAt(schedule, seconds),
            temperatureKelvin: BENCH_TEMPERATURE_KELVIN + (REACTANT_TEMPERATURE_KELVIN - BENCH_TEMPERATURE_KELVIN) * (1 - cooling),
            moleculesKeepingDrift: NO_MOLECULES
        };
    }

    private computeForces(seconds: number): void {
        const productWeight = productWeightAt(this.input.schedule, seconds);

        this.system.forces.fill(0);
        this.interatomicForces.accumulate(productWeight);
        this.approachGuides.accumulate(seconds);
        this.activationShaping.accumulate(seconds, productWeight);
        this.landingGuides.accumulate(seconds, airFrictionPerSecondAt(this.input.schedule, seconds));
    }

    private kick(): void {
        const { atoms, velocities, forces } = this.system;

        for (let slot = 0; slot < velocities.length; slot++) {
            velocities[slot] += (0.5 * STEP_TIME_UNITS * forces[slot]) / atoms[Math.floor(slot / 3)].mass;
        }
    }

    private drift(): void {
        const { positions, velocities } = this.system;

        for (let slot = 0; slot < positions.length; slot++) {
            positions[slot] += 0.5 * STEP_TIME_UNITS * velocities[slot];
        }
    }

    private warm(): void {
        const { atoms, velocities } = this.system;
        const moleculeDrift = new Vector3();

        atoms.forEach((atom, index) => {
            const spread = Math.sqrt((BOLTZMANN_KILOJOULES_PER_MOLE_KELVIN * REACTANT_TEMPERATURE_KELVIN) / atom.mass);

            for (let axis = 0; axis < 3; axis++) {
                velocities[index * 3 + axis] = spread * this.draws.standardNormal();
            }
        });

        for (const { atomIndices, mass } of this.reactants) {
            this.system.massWeightedMeanOf(atomIndices, mass, velocities, moleculeDrift);

            for (const index of atomIndices) {
                velocities[index * 3] -= moleculeDrift.x;
                velocities[index * 3 + 1] -= moleculeDrift.y;
                velocities[index * 3 + 2] -= moleculeDrift.z;
            }
        }
    }
}

function coolingAt(schedule: DynamicsSchedule, seconds: number): number {
    return smoothProgressBetween(schedule.releaseSeconds + COOLING_AFTER_RELEASE.startSeconds, schedule.releaseSeconds + COOLING_AFTER_RELEASE.fullSeconds, seconds);
}

function airFrictionPerSecondAt(schedule: DynamicsSchedule, seconds: number): number {
    return AIR_DRAG_PER_SECOND + FRICTION_PER_SECOND.cooling * coolingAt(schedule, seconds);
}