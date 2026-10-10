import { Mesh, WebGLRenderer, WebGLRenderTarget } from "three";
import { Disposable } from "../core/disposal-scope";
import { ReactionTrace } from "../animation/reaction-trace";
import { GasAirflow } from "../gas/gas-airflow";
import { GasRider } from "../gas/gas-rider";
import { SparkField } from "./spark-field";
import { SparkStreaks } from "./spark-streaks";
import { planSparkLaunches } from "./spark-launches";
import { LIFE_PER_COOLING } from "./spark-launch";
import { compileOffscreen } from "../rendering/shader-warm-up";

interface KeptSparks {
    readonly seconds: number;
    readonly state: WebGLRenderTarget;
}

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
    private kept: KeptSparks[] = [];

    constructor(private readonly renderer: WebGLRenderer) {
        this.field = new SparkField(renderer);
    }

    get mesh(): Mesh {
        return this.streaks.mesh;
    }

    begin(trace: ReactionTrace | null): void {
        const launches = trace === null ? [] : planSparkLaunches(trace);

        this.forget();
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

    ride(seconds: number, airflow: GasAirflow): void {
        this.rodeSinceShown = this.stepTo(seconds, airflow) > 0 || this.rodeSinceShown;
    }

    keep(seconds: number): void {
        if (this.launchCount === 0 || seconds > this.lastSparkSeconds || Math.abs(this.simulatedSeconds - seconds) > STEP_TOLERANCE_SECONDS) {
            return;
        }

        this.dropKept(seconds);
        this.kept.push({ seconds, state: this.field.capture() });
    }

    resume(seconds: number): void {
        const kept = this.kept.find((entry) => Math.abs(entry.seconds - seconds) <= STEP_TOLERANCE_SECONDS);

        if (kept !== undefined) {
            this.field.restore(kept.state);
        }

        if (kept !== undefined || seconds > this.lastSparkSeconds) {
            this.simulatedSeconds = seconds;
            this.rodeSinceShown = true;
        }
    }

    forget(): void {
        for (const { state } of this.kept) {
            state.dispose();
        }

        this.kept = [];
    }

    setElapsed(elapsedSeconds: number, airflow: GasAirflow | null): boolean {
        const reachableSeconds = airflow === null ? elapsedSeconds : Math.min(elapsedSeconds, airflow.simulatedSeconds);
        const stepped = this.stepTo(reachableSeconds, airflow) > 0 || this.rodeSinceShown;

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

    warmUp(): void {
        compileOffscreen(this.renderer, [...this.field.materials, this.streaks.material]);
    }

    end(): void {
        this.begin(null);
    }

    restoreContext(): void {
        this.forget();
        this.restart();
    }

    dispose(): void {
        this.forget();
        this.field.dispose();
        this.streaks.dispose();
    }

    private stepTo(seconds: number, airflow: GasAirflow | null): number {
        const targetSeconds = Math.min(seconds, this.lastSparkSeconds + STEP_SECONDS);
        let steps = 0;

        while (this.launchCount > 0 && this.simulatedSeconds + STEP_SECONDS <= targetSeconds + STEP_TOLERANCE_SECONDS && steps < MAX_STEPS_PER_FRAME) {
            this.simulatedSeconds += STEP_SECONDS;
            this.field.step(this.simulatedSeconds, STEP_SECONDS, airflow);
            steps++;
        }

        return steps;
    }

    private dropKept(seconds: number): void {
        for (const entry of this.kept.filter((kept) => Math.abs(kept.seconds - seconds) <= STEP_TOLERANCE_SECONDS)) {
            entry.state.dispose();
        }

        this.kept = this.kept.filter((kept) => Math.abs(kept.seconds - seconds) > STEP_TOLERANCE_SECONDS);
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
