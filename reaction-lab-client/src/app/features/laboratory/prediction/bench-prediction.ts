import { computed, inject, Injectable, signal } from "@angular/core";
import { takeUntilDestroyed, toObservable } from "@angular/core/rxjs-interop";
import { catchError, EMPTY, filter, finalize, Observable, switchMap, tap } from "rxjs";
import { ChemistryEngine } from "../../../core/chemistry/chemistry-engine";
import { PredictedReaction, PredictionResult, StandardState } from "../../../core/chemistry/chemistry-contracts";
import { ReactionStore } from "../../../state/reaction-store";
import { UiStore } from "../../../state/ui-store";
import { WorkspaceStore } from "../../../state/workspace-store";
import { SubstanceSummary } from "../../../data/substances/substance";
import { predictedReactionViews } from "./predicted-reaction-view";

@Injectable()
export class BenchPrediction {
    private readonly engine = inject(ChemistryEngine);
    private readonly workspace = inject(WorkspaceStore);
    private readonly reactions = inject(ReactionStore);
    private readonly ui = inject(UiStore);
    private readonly predicted = signal<readonly PredictedReaction[]>([]);
    private readonly formulas = computed(
        () => [...new Set(this.workspace.entries().map((entry) => entry.substance.formula))].sort(),
        { equal: (first, second) => first.join(' ') === second.join(' ') });

    readonly loading = signal(false);
    readonly failed = signal(false);
    readonly views = computed(() =>
        predictedReactionViews(this.predicted(), this.reactions.scored().map((scored) => scored.reaction)));

    constructor() {
        toObservable(computed(() => (this.ui.reactionsOpen() ? this.formulas() : null))).pipe(
            filter((formulas): formulas is string[] => formulas !== null),
            switchMap((formulas) => this.predict(formulas)),
            takeUntilDestroyed()
        ).subscribe();
    }

    private predict(formulas: readonly string[]): Observable<PredictionResult> {
        if (formulas.length === 0) {
            this.predicted.set([]);

            return EMPTY;
        }

        this.loading.set(true);
        this.failed.set(false);

        return this.engine.predict(formulas, standardStatesOf(this.workspace.entries().map((entry) => entry.substance))).pipe(
            tap({
                next: (result) => {
                    this.predicted.set(result.reactions ?? []);
                    this.failed.set(result.error !== undefined);
                },
                error: () => this.failed.set(true)
            }),
            catchError(() => EMPTY),
            finalize(() => this.loading.set(false))
        );
    }
}

function standardStatesOf(substances: readonly SubstanceSummary[]): Record<string, StandardState> {
    const standard: Record<string, StandardState> = {};

    for (const { formula, stateAtRoomTemperature: state } of substances) {
        if (state !== null && state !== 'Plasma') {
            standard[formula] = state;
        }
    }

    return standard;
}
