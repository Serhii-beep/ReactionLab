import { Group } from "three";
import { Disposable } from "../core/disposal-scope";
import { QualityTier } from "../rendering/quality";
import { QualityLevel } from "../performance/quality-governor";
import { ReactionGas } from "../gas/reaction-gas";
import { GasRun } from "../gas/gas-run";
import { ReactionSparks } from "../sparks/reaction-sparks";
import { ReactionTrace } from "../animation/reaction-trace";

const SPARK_BUDGET_BY_TIER: Readonly<Record<QualityTier, number>> = { high: 1, medium: 0.6, low: 0.35 };

export class ReactionEffects implements Disposable {
    readonly root = new Group();

    constructor(private readonly gas: ReactionGas, private readonly sparks: ReactionSparks) {
        this.root.name = 'reaction-effects';
        this.root.add(sparks.mesh);
    }

    begin(trace: ReactionTrace, gasRun: GasRun | null): void {
        this.gas.begin(gasRun);
        this.sparks.begin(trace);
    }

    setElapsed(elapsedSeconds: number): boolean {
        this.sparks.rewindTo(elapsedSeconds);

        const gasStepped = this.gas.setElapsed(elapsedSeconds);
        const sparksStepped = this.sparks.setElapsed(elapsedSeconds, this.gas.airflow);

        return gasStepped || sparksStepped;
    }

    setQuality(level: QualityLevel): void {
        this.gas.setQuality(level.tier);
        this.sparks.setBudget(SPARK_BUDGET_BY_TIER[level.tier] * level.resolutionScale);
    }

    warmUp(): void {
        this.gas.warmUp();
        this.sparks.warmUp();
    }

    restoreContext(): void {
        this.gas.restoreContext();
        this.sparks.restoreContext();
    }

    end(): void {
        this.gas.end();
        this.sparks.end();
    }

    dispose(): void {
        this.gas.dispose();
        this.sparks.dispose();
        this.root.removeFromParent();
    }
}
