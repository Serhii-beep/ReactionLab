import { Mesh, WebGLRenderer } from "three";
import { Disposable } from "../core/disposal-scope";
import { ReactionTrace } from "../animation/reaction-trace";
import { GasAirflow } from "../gas/gas-airflow";
import { GasRider } from "../gas/gas-rider";
import { SparkField } from "./spark-field";
import { SparkStreaks } from "./spark-streaks";
import { planSparkLaunches } from "./spark-launches";
import { LIFE_PER_COOLING } from "./spark-launch";

const STEP_SECONDS = 1 / 60;
const STEP_TOLERANCE_SECONDS = 1e-6;
const MAX_STEPS_PER_FRAME = 240;

export class ReactionSparks implements Disposable, GasRider {
    private readonly field: SparkField;
    private readonly streaks = new SparkStreaks();
    private launchCount = 0;
    private lastSparkSeconds = 0;
    private simulatedSeconds = 0;
    private budgetScale = 1;
    private rodeSinceShown = false;

    constructor(renderer: WebGLRenderer) {
        this.field = new SparkField(renderer);
    }

    get mesh(): Mesh {
        return this.streaks.mesh;
    }

    begin(trace: ReactionTrace | null): void {
        const launches = trace === null ? [] : planSparkLaunches(trace);

        this.launchCount = launches.length;
        this.lastSparkSeconds = launches.reduce((latest, launch) => Math.max(latest, launch.seconds + launch.coolingSeconds * LIFE_PER_COOLING), 0);

        if (launches.length > 0) {
            this.field.load(launches);
        }

        this.restart();
    }

    rewindTo(elapsedSeconds: number): void {
        if (this.launchCount > 0 && elapsedSeconds < this.simulatedSeconds - STEP_TOLERANCE_SECONDS) {
            this.restart();
        }
    }

    ride(seconds: number, stepSeconds: number, airflow: GasAirflow): void {
        this.rodeSinceShown = this.stepTo(seconds, stepSeconds, airflow) > 0 || this.rodeSinceShown;
    }

    setElapsed(elapsedSeconds: number, airflow: GasAirflow | null): boolean {
        const reachableSeconds = airflow === null ? elapsedSeconds : Math.min(elapsedSeconds, airflow.simulatedSeconds);
        const stepped = this.stepTo(reachableSeconds, STEP_SECONDS, airflow) > 0 || this.rodeSinceShown;

        this.rodeSinceShown = false;

        if (stepped) {
            this.show();
        }

        return stepped;
    }

    setBudget(budgetScale: number): void {
        this.budgetScale = budgetScale;

        if (this.launchCount > 0) {
            this.show();
        }
    }

    end(): void {
        this.begin(null);
    }

    restoreContext(): void {
        this.restart();
    }

    dispose(): void {
        this.field.dispose();
        this.streaks.dispose();
    }

    private stepTo(seconds: number, stepSeconds: number, airflow: GasAirflow | null): number {
        const targetSeconds = Math.min(seconds, this.lastSparkSeconds + stepSeconds);
        let steps = 0;

        while (this.launchCount > 0 && this.simulatedSeconds + stepSeconds <= targetSeconds + STEP_TOLERANCE_SECONDS && steps < MAX_STEPS_PER_FRAME) {
            this.simulatedSeconds += stepSeconds;
            this.field.step(this.simulatedSeconds, stepSeconds, airflow);
            steps++;
        }

        return steps;
    }

    private restart(): void {
        this.simulatedSeconds = 0;

        if (this.launchCount === 0) {
            this.streaks.hide();

            return;
        }

        this.field.reset();
        this.show();
    }

    private show(): void {
        if (this.simulatedSeconds > this.lastSparkSeconds) {
            this.streaks.hide();

            return;
        }

        this.streaks.show(this.field, Math.max(Math.round(this.launchCount * this.budgetScale), 1));
    }
}
