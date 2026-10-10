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

const STEP_SECONDS = 1 / 60;
const STEP_TOLERANCE_SECONDS = 1e-6;
const VOXEL_STEPS_PER_FRAME = 4 * 128 ** 3;
const TAIL_SECONDS = 4;
const TAIL_FADE_SECONDS = 1.5;
const SMALLEST_SIDE_ANGSTROM = 6;
const SIDE_PER_REACH = 5;
const VOXELS_PER_SIDE_BY_TIER: Readonly<Record<QualityTier, number>> = { high: 128, medium: 64, low: 0 };

export class ReactionGas implements Disposable {
    private readonly solver: GasSolver;
    private run: GasRun | null = null;
    private feed: GasFeed | null = null;
    private voxelsPerSide = VOXELS_PER_SIDE_BY_TIER.high;
    private simulatedSeconds = 0;

    constructor(renderer: WebGLRenderer, private readonly volume: GasVolume) {
        this.solver = new GasSolver(renderer);
    }

    get airflow(): GasAirflow | null {
        return this.volume.enabled ? this.currentAirflow() : null;
    }

    begin(run: GasRun | null): void {
        this.run = run;
        this.feed = run === null ? null : new GasFeed(run);
        this.restart();
    }

    setElapsed(elapsedSeconds: number, rider: GasRider): boolean {
        const { feed, run } = this;

        if (feed === null || run === null) {
            return false;
        }

        if (elapsedSeconds < this.simulatedSeconds - STEP_TOLERANCE_SECONDS) {
            this.restart();
        }

        if (!this.volume.enabled) {
            return false;
        }

        const tailEndSeconds = run.trace.schedule.durationSeconds + TAIL_SECONDS;
        const steps = this.stepTo(Math.min(elapsedSeconds, tailEndSeconds), feed, rider);

        if (this.simulatedSeconds + STEP_SECONDS > tailEndSeconds + STEP_TOLERANCE_SECONDS) {
            this.volume.hide();
            this.solver.release();
        } else if (steps > 0) {
            this.solver.light();
            this.volume.track(this.solver);
            this.volume.fade(1 - smoothProgressBetween(tailEndSeconds - TAIL_FADE_SECONDS, tailEndSeconds, this.simulatedSeconds));
        }

        return steps > 0;
    }

    setQuality(tier: QualityTier): void {
        const voxelsPerSide = VOXELS_PER_SIDE_BY_TIER[tier];
        const coarser = voxelsPerSide < this.voxelsPerSide;

        this.voxelsPerSide = voxelsPerSide;

        if (coarser && (voxelsPerSide === 0 || this.volume.enabled)) {
            this.restart();
        }
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
    }

    private stepTo(targetSeconds: number, feed: GasFeed, rider: GasRider): number {
        const maxSteps = Math.max(Math.floor(VOXEL_STEPS_PER_FRAME / this.voxelsPerSide ** 3), 1);
        let steps = 0;

        while (this.simulatedSeconds + STEP_SECONDS <= targetSeconds + STEP_TOLERANCE_SECONDS && steps < maxSteps) {
            const middleSeconds = this.simulatedSeconds + STEP_SECONDS / 2;

            this.solver.setSources(feed.sourcesAt(middleSeconds));
            this.solver.setObstacles(feed.obstaclesAt(middleSeconds));
            this.solver.step(STEP_SECONDS);
            this.simulatedSeconds += STEP_SECONDS;
            rider.ride(this.simulatedSeconds, STEP_SECONDS, this.currentAirflow());
            steps++;
        }

        return steps;
    }

    private currentAirflow(): GasAirflow {
        const { solver } = this;

        return { simulatedSeconds: this.simulatedSeconds, velocity: solver.velocity, scalars: solver.scalars, grid: solver.grid, box: solver.box };
    }

    private restart(): void {
        const { run, feed } = this;

        this.simulatedSeconds = 0;

        if (run === null || feed === null || this.voxelsPerSide === 0) {
            this.volume.hide();
            this.solver.release();

            return;
        }

        const { schedule, meeting } = run.trace;
        const sideAngstrom = Math.max(SMALLEST_SIDE_ANGSTROM, SIDE_PER_REACH * feed.reachAt(schedule.releaseSeconds));

        this.solver.allocate(this.voxelsPerSide);
        this.solver.place(new Vector3(meeting.x - sideAngstrom / 2, 0, meeting.z - sideAngstrom / 2), sideAngstrom);
        this.volume.show(this.solver, GAS_LOOKS[run.look], run.cloud?.albedo ?? null);
    }
}
