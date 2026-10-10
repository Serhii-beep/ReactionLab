import { Vector3, WebGLRenderer } from "three";
import { Disposable } from "../core/disposal-scope";
import { QualityTier } from "../rendering/quality";
import { GasSolver } from "./gas-solver";
import { GasVolume } from "./gas-volume";
import { GasFeed } from "./gas-feed";
import { GasRun } from "./gas-run";
import { GAS_LOOKS } from "./gas-look";
import { GasAirflow } from "./gas-airflow";
import { GasRider } from "./gas-rider";
import { smoothProgressBetween } from "../animation/easing";
import { GasGlow } from "./gas-glow";
import { GasSnapshots } from "./gas-snapshots";
import { ReactionLight } from "../rendering/reaction-light";
import { compileOffscreen } from "../rendering/shader-warm-up";

interface GasTier {
    readonly voxelsPerSide: number;
    readonly stepSeconds: number;
    readonly snapshotCount: number;
}

const STEP_TOLERANCE_SECONDS = 1e-6;
const VOXEL_STEPS_PER_FRAME = 4 * 128 ** 3;
const TAIL_SECONDS = 4;
const TAIL_FADE_SECONDS = 1.5;
const SMALLEST_SIDE_ANGSTROM = 6;
const SIDE_PER_REACH = 5;
const GAS_TIER_BY_QUALITY: Readonly<Record<QualityTier, GasTier>> = {
    high: { voxelsPerSide: 128, stepSeconds: 1 / 60, snapshotCount: 4 },
    medium: { voxelsPerSide: 64, stepSeconds: 1 / 30, snapshotCount: 0 },
    low: { voxelsPerSide: 0, stepSeconds: 1 / 30, snapshotCount: 0 }
};

export class ReactionGas implements Disposable {
    private readonly solver: GasSolver;
    private readonly glow: GasGlow;
    private readonly snapshots: GasSnapshots;
    private run: GasRun | null = null;
    private feed: GasFeed | null = null;
    private requestedTier = GAS_TIER_BY_QUALITY.high;
    private runningTier = GAS_TIER_BY_QUALITY.high;
    private simulatedSeconds = 0;

    constructor(private readonly renderer: WebGLRenderer, private readonly volume: GasVolume, private readonly rider: GasRider, reactionLight: ReactionLight) {
        this.solver = new GasSolver(renderer);
        this.glow = new GasGlow(renderer, (sources) => reactionLight.glowFromGas(sources));
        this.snapshots = new GasSnapshots(renderer);
    }

    get airflow(): GasAirflow | null {
        return this.volume.enabled ? this.currentAirflow() : null;
    }

    begin(run: GasRun | null): void {
        this.run = run;
        this.feed = run === null ? null : new GasFeed(run);
        this.restart();
    }

    setElapsed(elapsedSeconds: number): boolean {
        const { feed, run } = this;

        if (feed === null || run === null) {
            return false;
        }

        const tailEndSeconds = run.trace.schedule.durationSeconds + TAIL_SECONDS;
        const reset = this.catchUpTo(elapsedSeconds, tailEndSeconds, run, feed);
        const glowArrived = this.glow.poll();

        if (!this.volume.enabled) {
            return reset || glowArrived;
        }

        const steps = this.stepTo(Math.min(elapsedSeconds, tailEndSeconds), feed);

        if (this.simulatedSeconds + this.runningTier.stepSeconds > tailEndSeconds + STEP_TOLERANCE_SECONDS) {
            this.hide();
        } else if (steps > 0 || reset) {
            this.refresh(tailEndSeconds);
        }

        return steps > 0 || reset || glowArrived;
    }

    setQuality(tier: QualityTier): void {
        const requestedTier = GAS_TIER_BY_QUALITY[tier];
        const coarser = requestedTier.voxelsPerSide < this.runningTier.voxelsPerSide && this.volume.enabled;

        this.requestedTier = requestedTier;

        if (coarser) {
            this.restart();
        }
    }

    warmUp(): void {
        compileOffscreen(this.renderer, [...this.solver.materials, ...this.glow.materials, ...this.snapshots.materials]);
        this.volume.warmUp(this.renderer);
    }

    end(): void {
        this.begin(null);
    }

    restoreContext(): void {
        this.volume.forgetNoise();
        this.restart();
    }

    dispose(): void {
        this.solver.dispose();
        this.glow.dispose();
        this.snapshots.dispose();
    }

    private catchUpTo(elapsedSeconds: number, tailEndSeconds: number, run: GasRun, feed: GasFeed): boolean {
        const rewinding = elapsedSeconds < this.simulatedSeconds - STEP_TOLERANCE_SECONDS;
        const appearing = this.runningTier.voxelsPerSide === 0 && this.requestedTier.voxelsPerSide > 0 && elapsedSeconds < tailEndSeconds;

        if (appearing || (rewinding && this.requestedTier !== this.runningTier)) {
            this.restart();

            return true;
        }

        if (rewinding) {
            this.rewind(elapsedSeconds, run, feed);
        }

        return rewinding;
    }

    private rewind(elapsedSeconds: number, run: GasRun, feed: GasFeed): void {
        const snapshot = this.snapshots.latestBy(elapsedSeconds);

        if (!this.volume.enabled) {
            this.reveal(run, feed);
        } else if (snapshot === null) {
            this.solver.clear();
        }

        this.simulatedSeconds = snapshot?.seconds ?? 0;

        if (snapshot !== null) {
            this.snapshots.restore(snapshot, this.solver.persistentFields);
            this.solver.resetFlow();
            this.rider.resume(snapshot.seconds);
        }
    }

    private stepTo(targetSeconds: number, feed: GasFeed): number {
        const { stepSeconds, voxelsPerSide } = this.runningTier;
        const maxSteps = Math.max(Math.floor(VOXEL_STEPS_PER_FRAME / voxelsPerSide ** 3), 1);
        let steps = 0;

        while (this.simulatedSeconds + stepSeconds <= targetSeconds + STEP_TOLERANCE_SECONDS && steps < maxSteps) {
            const middleSeconds = this.simulatedSeconds + stepSeconds / 2;

            this.solver.setSources(feed.sourcesAt(middleSeconds));
            this.solver.setObstacles(feed.obstaclesAt(middleSeconds));
            this.solver.step(stepSeconds);
            this.simulatedSeconds += stepSeconds;
            this.rider.ride(this.simulatedSeconds, this.currentAirflow());
            this.keepIfDue();
            steps++;
        }

        return steps;
    }

    private keepIfDue(): void {
        if (this.snapshots.due(this.simulatedSeconds)) {
            this.snapshots.take(this.simulatedSeconds, this.solver.persistentFields);
            this.rider.keep(this.simulatedSeconds);
        }
    }

    private refresh(tailEndSeconds: number): void {
        const visibleShare = 1 - smoothProgressBetween(tailEndSeconds - TAIL_FADE_SECONDS, tailEndSeconds, this.simulatedSeconds);

        this.solver.light();
        this.volume.track(this.solver);
        this.volume.fade(visibleShare);
        this.glow.measure(this.solver, visibleShare);
    }

    private currentAirflow(): GasAirflow {
        const { solver } = this;

        return { simulatedSeconds: this.simulatedSeconds, velocity: solver.velocity, scalars: solver.scalars, grid: solver.grid, box: solver.box };
    }

    private restart(): void {
        const { run, feed } = this;

        this.simulatedSeconds = 0;
        this.runningTier = this.requestedTier;
        this.rider.forget();

        if (run === null || feed === null || this.runningTier.voxelsPerSide === 0) {
            this.snapshots.release();
            this.hide();

            return;
        }

        this.snapshots.plan(run.trace.schedule.durationSeconds, this.runningTier.snapshotCount);
        this.reveal(run, feed);
    }

    private reveal(run: GasRun, feed: GasFeed): void {
        const { schedule, meeting } = run.trace;
        const look = GAS_LOOKS[run.look];
        const sideAngstrom = Math.max(SMALLEST_SIDE_ANGSTROM, SIDE_PER_REACH * feed.reachAt(schedule.releaseSeconds));

        this.solver.allocate(this.runningTier.voxelsPerSide);
        this.solver.place(new Vector3(meeting.x - sideAngstrom / 2, 0, meeting.z - sideAngstrom / 2), sideAngstrom, look);
        this.volume.show(this.solver, look, run.particles);
        this.glow.prepare(look, run.particles);
    }

    private hide(): void {
        this.volume.hide();
        this.solver.release();
        this.glow.forget();
    }
}
