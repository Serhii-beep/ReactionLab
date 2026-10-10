import { Vector3 } from "three";
import { ReactionTrace, RecordedBond } from "../animation/reaction-trace";
import { RandomSource, seededRandom, standardNormalSource } from "../core/seeded-random";
import { FLAME_CEILING_KELVIN } from "../core/matter";
import { DRAPER_POINT_KELVIN } from "../rendering/incandescence";
import { SPARK_SLOTS, SparkLaunch } from "./spark-launch";
import { SPARK_STYLES, SparkStyle, ValueSpread } from "./spark-styles";

const UP = new Vector3(0, 1, 0);
const CONTACT_LEAD_SECONDS = 0.2;
const CONTACT_SCAN_SECONDS = 1 / 120;
const CONTACT_CLOSING_ANGSTROM = 0.02;
const SNAP_STRETCH = 1.3;
const SNAP_SCAN_SECONDS = 1 / 240;
const SNAP_SCAN_PAST_SWITCH_SECONDS = 0.3;
const BURST_HEAT_DELAY_SECONDS = 0.06;
const BURST_UPWARD_BIAS = 0.35;
const EMBER_SCAN_SECONDS = 1 / 60;
const EMBER_SURFACE_SHARE = 0.95;
const EMBER_CARRIED_SHARE = 0.75;
const EMBER_CARRIED_LIMIT_ANGSTROM_PER_SECOND = 2;
const EMBER_RISE_ANGSTROM_PER_SECOND = 0.35;
const VELOCITY_SPAN_SECONDS = 1 / 120;
const CONTACT_TANGENT_SHARE = 0.85;
const SNAP_JET_SPREAD = 0.45;

export function planSparkLaunches(trace: ReactionTrace): SparkLaunch[] {
    return new SparkPlanner(trace).plan();
}

class SparkPlanner {
    private readonly uniform: RandomSource;
    private readonly normal: RandomSource;
    private readonly launches: SparkLaunch[] = [];
    private readonly first = new Vector3();
    private readonly second = new Vector3();
    private readonly ahead = new Vector3();
    private readonly behind = new Vector3();

    constructor(private readonly trace: ReactionTrace) {
        this.uniform = seededRandom(`${trace.randomSeed}:sparks`);
        this.normal = standardNormalSource(this.uniform);
    }

    plan(): SparkLaunch[] {
        const { formingBonds, breakingBonds, reactantUnitIds, atomCount } = this.trace;

        for (const bond of formingBonds) {
            if (reactantUnitIds[bond.firstAtomIndex] !== reactantUnitIds[bond.secondAtomIndex]) {
                this.contact(bond);
            }

            this.burst(bond);
        }

        for (const bond of breakingBonds) {
            this.snap(bond);
        }

        for (let atomIndex = 0; atomIndex < atomCount; atomIndex++) {
            this.embers(atomIndex);
        }

        return this.shuffled().slice(0, SPARK_SLOTS);
    }

    private contact(bond: RecordedBond): void {
        const style = SPARK_STYLES.contact;
        const seconds = this.contactSecondsOf(bond);
        const kelvin = this.hottestOf(bond, this.trace.schedule.switchStartSeconds);
        const { radiusAt } = this.trace;
        const firstRadius = radiusAt(bond.firstAtomIndex, seconds);
        const { first, second } = this.placesAt(bond, seconds);
        const normal = second.clone().sub(first).normalize();
        const contact = first.clone().lerp(second, firstRadius / (firstRadius + radiusAt(bond.secondAtomIndex, seconds)));
        const carried = this.bondVelocityAt(bond, seconds);

        for (let index = 0; index < this.countOf(style, kelvin); index++) {
            const spray = this.direction();

            spray.addScaledVector(normal, -CONTACT_TANGENT_SHARE * spray.dot(normal)).normalize();
            this.launch(style, seconds, contact, spray.multiplyScalar(this.within(style.speedAngstromPerSecond)).add(carried), kelvin);
        }
    }

    private snap(bond: RecordedBond): void {
        const style = SPARK_STYLES.snap;
        const seconds = this.snapSecondsOf(bond);

        if (seconds === null) {
            return;
        }

        const kelvin = this.hottestOf(bond, seconds);
        const { first, second } = this.placesAt(bond, seconds);
        const axis = second.clone().sub(first).normalize();
        const midpoint = first.clone().add(second).multiplyScalar(0.5);

        for (let index = 0; index < this.countOf(style, kelvin); index++) {
            const jet = axis.clone().multiplyScalar(index % 2 === 0 ? 1 : -1).addScaledVector(this.direction(), SNAP_JET_SPREAD).normalize();

            this.launch(style, seconds, midpoint, jet.multiplyScalar(this.within(style.speedAngstromPerSecond)), kelvin);
        }
    }

    private burst(bond: RecordedBond): void {
        const style = SPARK_STYLES.burst;
        const seconds = this.trace.schedule.releaseSeconds;
        const kelvin = this.hottestOf(bond, seconds + BURST_HEAT_DELAY_SECONDS);
        const { first, second } = this.placesAt(bond, seconds);
        const midpoint = first.clone().add(second).multiplyScalar(0.5);
        const carried = this.bondVelocityAt(bond, seconds);

        for (let index = 0; index < this.countOf(style, kelvin); index++) {
            const outward = this.direction().addScaledVector(UP, BURST_UPWARD_BIAS).normalize();

            this.launch(style, seconds, midpoint, outward.multiplyScalar(this.within(style.speedAngstromPerSecond)).add(carried), kelvin);
        }
    }

    private embers(atomIndex: number): void {
        const style = SPARK_STYLES.ember;
        const { schedule, temperatureKelvinAt } = this.trace;

        for (let seconds = schedule.collisionStartSeconds; seconds <= schedule.durationSeconds; seconds += EMBER_SCAN_SECONDS) {
            const kelvin = temperatureKelvinAt(atomIndex, seconds);

            if (this.uniform() < style.countAtFullHeat * hotnessOf(kelvin) * EMBER_SCAN_SECONDS) {
                this.shedEmber(style, atomIndex, seconds, kelvin);
            }
        }
    }

    private shedEmber(style: SparkStyle, atomIndex: number, seconds: number, kelvin: number): void {
        const outward = this.direction();
        const surface = this.trace.placeAt(atomIndex, seconds, new Vector3()).addScaledVector(outward, this.trace.radiusAt(atomIndex, seconds) * EMBER_SURFACE_SHARE);
        const drift = this.atomVelocityAt(atomIndex, seconds).multiplyScalar(EMBER_CARRIED_SHARE).clampLength(0, EMBER_CARRIED_LIMIT_ANGSTROM_PER_SECOND)
            .addScaledVector(outward, this.within(style.speedAngstromPerSecond))
            .addScaledVector(UP, EMBER_RISE_ANGSTROM_PER_SECOND);

        this.launch(style, seconds, surface, drift, kelvin);
    }

    private contactSecondsOf(bond: RecordedBond): number {
        const { schedule, radiusAt } = this.trace;
        let closest = Infinity;
        let contactSeconds = schedule.collisionStartSeconds;

        for (let seconds = schedule.collisionStartSeconds - CONTACT_LEAD_SECONDS; seconds <= schedule.switchEndSeconds; seconds += CONTACT_SCAN_SECONDS) {
            const gap = this.lengthAt(bond, seconds) - radiusAt(bond.firstAtomIndex, seconds) - radiusAt(bond.secondAtomIndex, seconds);

            if (gap < closest - CONTACT_CLOSING_ANGSTROM) {
                closest = gap;
                contactSeconds = seconds;
            }

            if (gap < 0) {
                break;
            }
        }

        return contactSeconds;
    }

    private snapSecondsOf(bond: RecordedBond): number | null {
        const { schedule } = this.trace;
        const restLength = this.lengthAt(bond, 0);

        for (let seconds = schedule.collisionStartSeconds; seconds <= schedule.switchEndSeconds + SNAP_SCAN_PAST_SWITCH_SECONDS; seconds += SNAP_SCAN_SECONDS) {
            if (this.lengthAt(bond, seconds) > restLength * SNAP_STRETCH) {
                return seconds;
            }
        }

        return null;
    }

    private launch(style: SparkStyle, seconds: number, place: Vector3, velocity: Vector3, kelvin: number): void {
        this.launches.push({
            seconds: seconds + this.within(style.delaySeconds),
            place: place.clone(),
            velocity: velocity.clone(),
            kelvin,
            dragPerSecond: style.dragPerSecond,
            liftAngstromPerSecondSquared: style.liftAngstromPerSecondSquared,
            coolingSeconds: this.within(style.coolingSeconds),
            widthPixels: this.within(style.widthPixels)
        });
    }

    private hottestOf(bond: RecordedBond, seconds: number): number {
        const { temperatureKelvinAt } = this.trace;

        return Math.max(temperatureKelvinAt(bond.firstAtomIndex, seconds), temperatureKelvinAt(bond.secondAtomIndex, seconds));
    }

    private countOf(style: SparkStyle, kelvin: number): number {
        return Math.round(style.countAtFullHeat * hotnessOf(kelvin));
    }

    private placesAt(bond: RecordedBond, seconds: number): { readonly first: Vector3; readonly second: Vector3 } {
        this.trace.placeAt(bond.firstAtomIndex, seconds, this.first);
        this.trace.placeAt(bond.secondAtomIndex, seconds, this.second);

        return { first: this.first, second: this.second };
    }

    private lengthAt(bond: RecordedBond, seconds: number): number {
        const { first, second } = this.placesAt(bond, seconds);

        return first.distanceTo(second);
    }

    private atomVelocityAt(atomIndex: number, seconds: number): Vector3 {
        this.trace.placeAt(atomIndex, seconds + VELOCITY_SPAN_SECONDS, this.ahead);
        this.trace.placeAt(atomIndex, seconds - VELOCITY_SPAN_SECONDS, this.behind);

        return new Vector3().subVectors(this.ahead, this.behind).divideScalar(2 * VELOCITY_SPAN_SECONDS);
    }

    private bondVelocityAt(bond: RecordedBond, seconds: number): Vector3 {
        return this.atomVelocityAt(bond.firstAtomIndex, seconds).add(this.atomVelocityAt(bond.secondAtomIndex, seconds)).multiplyScalar(0.5);
    }

    private direction(): Vector3 {
        return new Vector3(this.normal(), this.normal(), this.normal()).normalize();
    }

    private within(spread: ValueSpread): number {
        return spread.minimum + (spread.maximum - spread.minimum) * this.uniform();
    }

    private shuffled(): SparkLaunch[] {
        const launches = [...this.launches];

        for (let index = launches.length - 1; index > 0; index--) {
            const swapIndex = Math.floor(this.uniform() * (index + 1));

            [launches[index], launches[swapIndex]] = [launches[swapIndex], launches[index]];
        }

        return launches;
    }
}

function hotnessOf(kelvin: number): number {
    return Math.min(Math.max((kelvin - DRAPER_POINT_KELVIN) / (FLAME_CEILING_KELVIN - DRAPER_POINT_KELVIN), 0), 1);
}
