import { Sphere, Vector3 } from "three";
import { BenchLayout, LayoutUnit, PlacedAtom, PlacedBond } from "../scene/bench-layout";
import { ROOM_TEMPERATURE_KELVIN } from "../core/matter";
import { latticeContactsOf } from "../scene/lattice-contacts";
import { seededRandom, standardNormalSource } from "../core/seeded-random";
import { DynamicsSchedule } from "./dynamics/dynamics-schedule";
import { easeInOutCubic, progressBetween, smoothProgressBetween } from "./easing";
import { PrecipitatePlan } from "./reaction-script";

export interface CrystalGrowthSources {
    readonly plan: PrecipitatePlan;
    readonly after: BenchLayout;
    readonly unitsAfter: readonly LayoutUnit[];
    readonly solution: Sphere;
    readonly schedule: DynamicsSchedule;
    readonly randomSeed: string;
}

interface CarriedBond {
    readonly bond: PlacedBond;
    readonly restSide: Vector3;
}

interface StandIn {
    readonly atoms: readonly PlacedAtom[];
    readonly bonds: readonly CarriedBond[];
    readonly rests: readonly Vector3[];
    readonly restRadii: readonly number[];
    readonly restCenter: Vector3;
}

interface StandInFlight extends StandIn {
    readonly start: Vector3;
    readonly control: Vector3;
    readonly appearSeconds: number;
    readonly arrivalSeconds: number;
    readonly bobPhase: number;
}

interface LatticeLock {
    readonly bond: PlacedBond;
    readonly flightIndices: readonly number[];
}

const FLIGHT_SECONDS = 0.7;
const APPEAR_SECONDS = 0.3;
const APPEAR_WINDOW = { startAfterRelease: -0.05, endAfterRelease: 0.45 };
const FIRST_ARRIVAL_AFTER_RELEASE_SECONDS = 0.7;
const LAST_ARRIVAL_BEFORE_END_SECONDS = 0.1;
const LOCK_SECONDS = 0.15;
const LANDING_SECONDS = 0.3;
const SOLUTION_REACH = { minimum: 0.3, maximum: 0.8 };
const LOWEST_START_ANGSTROM = 0.6;
const ARC_LIFT_SHARE = 0.35;
const BOB_ANGSTROM = 0.08;
const BOB_RADIANS_PER_SECOND = 3.7;
const CENTER_SCRATCH = new Vector3();

export class CrystalGrowth {
    readonly atoms: readonly PlacedAtom[];
    readonly bonds: readonly PlacedBond[];
    readonly latticeBonds: readonly PlacedBond[];

    private readonly flights: readonly StandInFlight[];
    private readonly locks: readonly LatticeLock[];

    constructor(private readonly sources: CrystalGrowthSources) {
        const offset = latticeOffsetOf(sources);
        const standIns = sources.plan.standIns.map((unit) => standInOf(unit, offset));
        const realAtoms = sources.after.atoms.filter((atom) => atom.substanceId === sources.plan.substanceId);

        this.atoms = standIns.flatMap((standIn) => standIn.atoms);
        this.bonds = standIns.flatMap((standIn) => standIn.bonds.map(({ bond }) => bond));
        this.flights = flightsOf(standIns, centroidOf(realAtoms), sources);
        this.locks = latticeLocksOf(this.flights, realAtoms, sources.after.bonds);
        this.latticeBonds = this.locks.map((lock) => lock.bond);
    }

    place(seconds: number): void {
        for (const flight of this.flights) {
            placeFlight(flight, seconds);
        }

        for (const { bond, flightIndices } of this.locks) {
            bond.strength = Math.min(...flightIndices.map((flightIndex) => this.lockAt(flightIndex, seconds)));
        }
    }

    private lockAt(flightIndex: number, seconds: number): number {
        if (flightIndex < 0) {
            const { durationSeconds } = this.sources.schedule;

            return smoothProgressBetween(durationSeconds - LANDING_SECONDS, durationSeconds, seconds);
        }

        const { arrivalSeconds } = this.flights[flightIndex];

        return smoothProgressBetween(arrivalSeconds, arrivalSeconds + LOCK_SECONDS, seconds);
    }
}

function standInOf(unit: LayoutUnit, offset: Vector3): StandIn {
    const atoms: PlacedAtom[] = unit.atoms.map((atom) => ({
        ...atom,
        position: atom.position.clone().add(offset),
        unitId: unit.id,
        substanceId: unit.substanceId,
        radius: atom.radius,
        temperatureKelvin: ROOM_TEMPERATURE_KELVIN
    }));
    const restCenter = centroidOf(atoms);

    return {
        atoms,
        bonds: unit.bonds.map((bond) => ({
            bond: { from: atoms[bond.from], to: atoms[bond.to], kind: bond.kind, sidePoint: restCenter.clone(), strength: 0, partial: false },
            restSide: restCenter.clone()
        })),
        rests: atoms.map((atom) => atom.position.clone()),
        restRadii: atoms.map((atom) => atom.radius),
        restCenter
    };
}

function flightsOf(standIns: readonly StandIn[], nucleus: Vector3, { solution, schedule, randomSeed }: CrystalGrowthSources): StandInFlight[] {
    const uniform = seededRandom(`${randomSeed}:stand-ins`);
    const normal = standardNormalSource(uniform);
    const ranks = rankByDistance(standIns.map((standIn) => standIn.restCenter.distanceTo(nucleus)));
    const firstArrival = schedule.releaseSeconds + FIRST_ARRIVAL_AFTER_RELEASE_SECONDS;
    const lastArrival = Math.max(schedule.durationSeconds - LAST_ARRIVAL_BEFORE_END_SECONDS, firstArrival);

    return standIns.map((standIn, index) => {
        const direction = new Vector3(normal(), normal(), normal()).normalize();
        const reach = solution.radius * (SOLUTION_REACH.minimum + (SOLUTION_REACH.maximum - SOLUTION_REACH.minimum) * uniform());
        const start = solution.center.clone().addScaledVector(direction, reach);

        start.y = Math.max(start.y, LOWEST_START_ANGSTROM);

        return {
            ...standIn,
            start,
            control: start.clone().lerp(standIn.restCenter, 0.5).add(new Vector3(0, start.distanceTo(standIn.restCenter) * ARC_LIFT_SHARE, 0)),
            appearSeconds: schedule.releaseSeconds + APPEAR_WINDOW.startAfterRelease + (APPEAR_WINDOW.endAfterRelease - APPEAR_WINDOW.startAfterRelease) * uniform(),
            arrivalSeconds: firstArrival + (lastArrival - firstArrival) * (ranks[index] / Math.max(standIns.length - 1, 1)),
            bobPhase: uniform() * Math.PI * 2
        };
    });
}

function placeFlight(flight: StandInFlight, seconds: number): void {
    const appear = smoothProgressBetween(flight.appearSeconds, flight.appearSeconds + APPEAR_SECONDS, seconds);
    const progress = easeInOutCubic(progressBetween(flight.arrivalSeconds - FLIGHT_SECONDS, flight.arrivalSeconds, seconds));
    const center = quadraticPointOf(flight.start, flight.control, flight.restCenter, progress, CENTER_SCRATCH);

    center.y += BOB_ANGSTROM * Math.sin(seconds * BOB_RADIANS_PER_SECOND + flight.bobPhase) * (1 - progress);
    flight.atoms.forEach((atom, index) => {
        atom.position.copy(flight.rests[index]).sub(flight.restCenter).add(center);
        atom.radius = flight.restRadii[index] * appear;
    });

    for (const { bond, restSide } of flight.bonds) {
        bond.sidePoint.copy(restSide).sub(flight.restCenter).add(center);
        bond.strength = appear;
    }
}

function latticeLocksOf(flights: readonly StandInFlight[], realAtoms: readonly PlacedAtom[], afterBonds: readonly PlacedBond[]): LatticeLock[] {
    const flightIndexByAtom = new Map(flights.flatMap((flight, flightIndex) => flight.atoms.map((atom) => [atom, flightIndex] as const)));
    const realSet = new Set(realAtoms);
    const bonds = [...afterBonds.filter((bond) => realSet.has(bond.from)), ...flights.flatMap((flight) => flight.bonds.map(({ bond }) => bond))];

    return latticeContactsOf([...realAtoms, ...flightIndexByAtom.keys()], bonds)
        .filter((bond) => flightIndexByAtom.has(bond.from) || flightIndexByAtom.has(bond.to))
        .map((bond) => ({ bond, flightIndices: [flightIndexByAtom.get(bond.from) ?? -1, flightIndexByAtom.get(bond.to) ?? -1] }));
}

function latticeOffsetOf({ plan, after, unitsAfter }: CrystalGrowthSources): Vector3 {
    const unit = unitsAfter.find((candidate) => candidate.substanceId === plan.substanceId);
    const placed = unit === undefined ? undefined : after.atoms.find((atom) => atom.unitId === unit.id);

    return unit === undefined || placed === undefined ? new Vector3() : placed.position.clone().sub(unit.atoms[0].position);
}

function rankByDistance(distances: readonly number[]): number[] {
    const order = distances.map((distance, index) => ({ distance, index })).sort((first, second) => first.distance - second.distance);
    const ranks = new Array<number>(distances.length);

    order.forEach(({ index }, rank) => {
        ranks[index] = rank;
    });

    return ranks;
}

function quadraticPointOf(start: Vector3, control: Vector3, end: Vector3, progress: number, target: Vector3): Vector3 {
    const rest = 1 - progress;

    return target.copy(start).multiplyScalar(rest * rest).addScaledVector(control, 2 * rest * progress).addScaledVector(end, progress * progress);
}

function centroidOf(atoms: readonly PlacedAtom[]): Vector3 {
    return atoms.reduce((sum, atom) => sum.add(atom.position), new Vector3()).divideScalar(Math.max(atoms.length, 1));
}
