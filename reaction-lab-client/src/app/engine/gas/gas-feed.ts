import { Vector3 } from "three";
import { ROOM_TEMPERATURE_KELVIN } from "../core/matter";
import { progressBetween } from "../animation/easing";
import { MAX_GAS_OBSTACLES, MAX_GAS_SOURCES } from "./gas-grid";
import { GasObstacle, GasRun, GasSource } from "./gas-run";
import { ReactionTrace, RecordedBond } from "../animation/reaction-trace";

interface WarmAtom {
    readonly atomIndex: number;
    readonly riseKelvin: number;
}

const HEATING_PER_SECOND = 8;
const HEAT_RADIUS_SHARE = 1.3;
const WARM_KELVIN = 5;
const BOND_SOURCE_RADIUS_ANGSTROM = 0.6;
const FLAME_PER_BOND = 1.5;
const WATER_SOURCE_RADIUS_ANGSTROM = 0.45;
const VAPOR_PER_WATER = 12;
const RELEASE_HALF_WIDTH_SECONDS = 0.1;
const VAPOR_SPREAD_SECONDS = 0.6;
const VELOCITY_SPAN_SECONDS = 1 / 60;
const FLAME_SOURCE_SLOTS = 16;
const VAPOR_SOURCE_SLOTS = 8;
const CLOUD_SOURCE_SLOTS = 12;
const CLOUD_SOURCE_RADIUS_ANGSTROM = 1;
const CLOUD_PER_ATOM = 5;
const CLOUD_LEAD_SECONDS = 0.1;
const CLOUD_GROWTH_SECONDS = 1.2;

export class GasFeed {
    private readonly aheadPlace = new Vector3();
    private readonly behindPlace = new Vector3();
    private readonly scratchPlace = new Vector3();
    private readonly flameBonds: readonly RecordedBond[];
    private readonly vaporOxygens: readonly number[];
    private readonly obstacleAtoms: readonly number[];
    private readonly cloudAtoms: readonly number[];
    private readonly trace: ReactionTrace;

    constructor(private readonly run: GasRun) {
        this.trace = run.trace;
        this.flameBonds = evenlyPicked(run.trace.formingBonds, FLAME_SOURCE_SLOTS);
        this.vaporOxygens = evenlyPicked(run.waterOxygens, VAPOR_SOURCE_SLOTS);
        this.obstacleAtoms = evenlyPicked(Array.from({ length: run.trace.atomCount }, (_, atomIndex) => atomIndex), MAX_GAS_OBSTACLES);
        this.cloudAtoms = evenlyPicked(run.cloud?.atomIndices ?? [], CLOUD_SOURCE_SLOTS);
    }

    reachAt(seconds: number): number {
        const { atomCount, meeting, placeAt, radiusAt } = this.trace;
        let reach = 0;

        for (let atomIndex = 0; atomIndex < atomCount; atomIndex++) {
            reach = Math.max(reach, placeAt(atomIndex, seconds, this.scratchPlace).distanceTo(meeting) + radiusAt(atomIndex, seconds));
        }

        return reach;
    }

    sourcesAt(seconds: number): GasSource[] {
        const formedSources = [...this.releaseSourcesAt(seconds), ...this.cloudSourcesAt(seconds)];

        return [...formedSources, ...this.heatSourcesAt(seconds, MAX_GAS_SOURCES - formedSources.length)];
    }

    obstaclesAt(seconds: number): GasObstacle[] {
        const { radiusAt, placeAt } = this.trace;

        return this.obstacleAtoms.map((atomIndex) => {
            placeAt(atomIndex, seconds + VELOCITY_SPAN_SECONDS, this.aheadPlace);
            placeAt(atomIndex, seconds - VELOCITY_SPAN_SECONDS, this.behindPlace);

            return {
                place: placeAt(atomIndex, seconds, new Vector3()),
                velocity: new Vector3().subVectors(this.aheadPlace, this.behindPlace).divideScalar(2 * VELOCITY_SPAN_SECONDS),
                radius: radiusAt(atomIndex, seconds)
            };
        });
    }

    private releaseSourcesAt(seconds: number): GasSource[] {
        const { waterOxygens, releasesHeat } = this.run;
        const { schedule, formingBonds, placeAt } = this.trace;

        if (!releasesHeat) {
            return [];
        }

        const formingPerSecond = formingRateAt(schedule.releaseSeconds - RELEASE_HALF_WIDTH_SECONDS, schedule.releaseSeconds + RELEASE_HALF_WIDTH_SECONDS, seconds);
        const steamingPerSecond = formingRateAt(schedule.releaseSeconds - RELEASE_HALF_WIDTH_SECONDS, schedule.releaseSeconds + VAPOR_SPREAD_SECONDS, seconds);
        const flamePerSecond = (FLAME_PER_BOND * formingPerSecond * formingBonds.length) / Math.max(this.flameBonds.length, 1);
        const vaporPerSecond = (VAPOR_PER_WATER * steamingPerSecond * waterOxygens.length) / Math.max(this.vaporOxygens.length, 1);
        const flames = flamePerSecond === 0 ? [] : this.flameBonds.map(({ firstAtomIndex, secondAtomIndex }) => formedSource(
            placeAt(firstAtomIndex, seconds, new Vector3()).add(placeAt(secondAtomIndex, seconds, this.scratchPlace)).multiplyScalar(0.5),
            BOND_SOURCE_RADIUS_ANGSTROM,
            0,
            flamePerSecond));
        const vapors = vaporPerSecond === 0 ? [] : this.vaporOxygens.map((atomIndex) => formedSource(placeAt(atomIndex, seconds, new Vector3()), WATER_SOURCE_RADIUS_ANGSTROM, vaporPerSecond, 0));

        return [...flames, ...vapors];
    }

    private cloudSourcesAt(seconds: number): GasSource[] {
        const { schedule, placeAt } = this.trace;
        const formedAtoms = this.run.cloud?.atomIndices.length ?? 0;
        const formingPerSecond = formingRateAt(schedule.releaseSeconds - CLOUD_LEAD_SECONDS, schedule.releaseSeconds + CLOUD_GROWTH_SECONDS, seconds);

        if (formingPerSecond === 0 || formedAtoms === 0) {
            return [];
        }

        const cloudPerSecond = (CLOUD_PER_ATOM * formingPerSecond * formedAtoms) / this.cloudAtoms.length;

        return this.cloudAtoms.map((atomIndex) => ({ ...formedSource(placeAt(atomIndex, seconds, new Vector3()), CLOUD_SOURCE_RADIUS_ANGSTROM, 0, 0), cloudPerSecond }));
    }

    private heatSourcesAt(seconds: number, slots: number): GasSource[] {
        const { atomCount, radiusAt, temperatureKelvinAt, placeAt } = this.trace;
        const warmAtoms: WarmAtom[] = Array.from({ length: atomCount }, (_, atomIndex) => ({ atomIndex, riseKelvin: temperatureKelvinAt(atomIndex, seconds) - ROOM_TEMPERATURE_KELVIN }))
            .filter(({ riseKelvin }) => riseKelvin > WARM_KELVIN);

        return warmAtoms.sort((first, second) => second.riseKelvin - first.riseKelvin).slice(0, slots).map(({ atomIndex, riseKelvin }) => ({
            place: placeAt(atomIndex, seconds, new Vector3()),
            radius: radiusAt(atomIndex, seconds) * HEAT_RADIUS_SHARE,
            targetKelvin: riseKelvin,
            heatingPerSecond: HEATING_PER_SECOND,
            vaporPerSecond: 0,
            flamePerSecond: 0,
            cloudPerSecond: 0
        }));
    }
}

function evenlyPicked<Item>(items: readonly Item[], slots: number): readonly Item[] {
    return items.length <= slots ? items : Array.from({ length: slots }, (_, slot) => items[Math.floor((slot * items.length) / slots)]);
}

function formedSource(place: Vector3, radius: number, vaporPerSecond: number, flamePerSecond: number): GasSource {
    return { place, radius, targetKelvin: 0, heatingPerSecond: 0, vaporPerSecond, flamePerSecond, cloudPerSecond: 0 };
}

function formingRateAt(startSeconds: number, endSeconds: number, seconds: number): number {
    const progress = progressBetween(startSeconds, endSeconds, seconds);

    return (6 * progress * (1 - progress)) / (endSeconds - startSeconds);
}
