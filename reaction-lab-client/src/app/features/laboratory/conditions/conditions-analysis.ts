import { computed, inject, Injectable, signal } from "@angular/core";
import { takeUntilDestroyed, toObservable } from "@angular/core/rxjs-interop";
import { auditTime, catchError, EMPTY, filter, map, Observable, of, switchMap, tap } from "rxjs";
import { ChemistryEngine } from "../../../core/chemistry/chemistry-engine";
import { ConditionsReactionEntry, ConditionsRequest, ReactionConditions } from "../../../core/chemistry/chemistry-contracts";
import { ReactionSummary } from "../../../data/reactions/reaction";
import { LabConditionsStore } from "../../../state/lab-conditions-store";
import { ReactionStore } from "../../../state/reaction-store";
import { UiStore } from "../../../state/ui-store";
import { WorkspaceStore } from "../../../state/workspace-store";
import { conditionsEntryOf, inWater, stoichiometricMolesOf } from "./conditions-entries";
import { ConditionsVerdict, conditionsVerdictOf } from "./conditions-verdict";
import { reactionEnergeticsOf } from "../run/reaction-energetics";

interface VerdictRequest {
    readonly reactions: readonly ReactionSummary[];
    readonly described: readonly ReactionSummary[];
    readonly request: ConditionsRequest;
}

interface VerdictBatch {
    readonly kelvin: number;
    readonly kilopascals: number;
    readonly verdictByReactionId: ReadonlyMap<string, ConditionsVerdict>;
}

interface FocusRequest {
    readonly reaction: ReactionSummary;
    readonly request: ConditionsRequest;
}

export interface FocusedConditions {
    readonly reaction: ReactionSummary;
    readonly conditions: ReactionConditions;
    readonly thermalEnergyKilojoulesPerMole: number;
    readonly kelvin: number;
    readonly kilopascals: number;
    readonly molesNow: readonly number[];
}

const SETTLE_MILLISECONDS = 40;
const NO_CONDITIONS: ReactionConditions = { speciesWithoutEntropy: [] };
const NO_VERDICTS: ReadonlyMap<string, ConditionsVerdict> = new Map();

@Injectable()
export class ConditionsAnalysis {
    private readonly engine = inject(ChemistryEngine);
    private readonly lab = inject(LabConditionsStore);
    private readonly reactions = inject(ReactionStore);
    private readonly workspace = inject(WorkspaceStore);
    private readonly ui = inject(UiStore);
    private readonly verdictBatch = signal<VerdictBatch | null>(null);

    readonly focusedReactionId = signal<string | null>(null);
    readonly focused = signal<FocusedConditions | null>(null);
    readonly focusFailed = signal(false);
    readonly verdicts = computed(() => this.verdictsAt(this.lab.kelvin(), this.lab.kilopascals()));
    readonly focusedReaction = computed(() => {
        const scored = this.reactions.scored();
        const chosen = scored.find((entry) => entry.reaction.id === this.focusedReactionId());

        return (chosen ?? scored.find((entry) => entry.readiness.runnable) ?? scored.at(0))?.reaction ?? null;
    });
    readonly verdictsSettled = computed(() => {
        const verdicts = this.verdicts();

        return this.reactions.scored().every((entry) => verdicts.has(entry.reaction.id));
    });
    readonly runnableCount = computed(() => {
        const verdicts = this.verdicts();

        return this.reactions.scored().filter((entry) => entry.readiness.runnable && verdicts.get(entry.reaction.id)?.blocksRun === false).length;
    });

    constructor() {
        toObservable(computed(() => this.verdictRequest(), { equal: sameVerdictRequest })).pipe(
            filter((request): request is VerdictRequest => request !== null),
            auditTime(SETTLE_MILLISECONDS),
            switchMap((request) => this.judge(request)),
            takeUntilDestroyed()
        ).subscribe();

        toObservable(computed(() => this.focusRequest())).pipe(
            auditTime(SETTLE_MILLISECONDS),
            switchMap((request) => this.study(request)),
            takeUntilDestroyed()
        ).subscribe();
    }

    focus(reactionId: string): void {
        this.focusedReactionId.set(reactionId);
        this.ui.openConditions();
    }

    verdictsAt(kelvin: number, kilopascals: number): ReadonlyMap<string, ConditionsVerdict> {
        const batch = this.verdictBatch();

        return batch !== null && batch.kelvin === kelvin && batch.kilopascals === kilopascals ? batch.verdictByReactionId : NO_VERDICTS;
    }

    private verdictRequest(): VerdictRequest | null {
        const reactions = this.reactions.scored().map((entry) => entry.reaction);
        const entries = reactions.map((reaction) => conditionsEntryOf(reaction, stoichiometricMolesOf));

        return reactions.length === 0 ? null : {
            reactions,
            described: reactions.filter((_, index) => entries[index] !== null),
            request: {
                kelvin: this.lab.kelvin(),
                kilopascals: this.lab.kilopascals(),
                reactions: entries.filter((entry): entry is ConditionsReactionEntry => entry !== null)
            }
        };
    }

    private focusRequest(): FocusRequest | null {
        const reaction = this.ui.conditionsOpen() ? this.focusedReaction() : null;
        const counts = this.workspace.counts();
        const entry = reaction === null ? null : conditionsEntryOf(reaction, (participant) => counts.get(participant.substanceId) ?? 0);

        return reaction === null || entry === null ? null : {
            reaction,
            request: { kelvin: this.lab.kelvin(), kilopascals: this.lab.kilopascals(), reactions: [entry], includeCurves: true }
        };
    }

    private judge(verdictRequest: VerdictRequest): Observable<unknown> {
        const { request } = verdictRequest;
        const described = request.reactions.length === 0
            ? of<readonly ReactionConditions[]>([])
            : this.engine.describeConditions(request).pipe(map((result) => result.reactions));

        return described.pipe(
            tap({
                next: (conditions) => this.acceptVerdicts(verdictRequest, conditions),
                error: () => this.acceptVerdicts(verdictRequest, [])
            }),
            catchError(() => EMPTY)
        );
    }

    private study(focusRequest: FocusRequest | null): Observable<unknown> {
        if (focusRequest === null) {
            this.focused.set(null);

            return EMPTY;
        }

        const { reaction, request } = focusRequest;

        return this.engine.describeConditions(request).pipe(
            tap({
                next: (result) => {
                    this.focusFailed.set(false);
                    this.focused.set({
                        reaction,
                        conditions: result.reactions[0],
                        thermalEnergyKilojoulesPerMole: result.thermalEnergyKilojoulesPerMole,
                        kelvin: request.kelvin,
                        kilopascals: request.kilopascals,
                        molesNow: request.reactions[0].participants.map((participant) => participant.moles)
                    });
                },
                error: () => this.focusFailed.set(true)
            }),
            catchError(() => EMPTY)
        );
    }

    private acceptVerdicts({ reactions, described, request }: VerdictRequest, conditions: readonly ReactionConditions[]): void {
        const conditionsByReactionId = new Map(described.map((reaction, index) => [reaction.id, conditions.at(index) ?? NO_CONDITIONS]));

        this.verdictBatch.set({
            kelvin: request.kelvin,
            kilopascals: request.kilopascals,
            verdictByReactionId: new Map(reactions.map((reaction) => [
                reaction.id,
                conditionsVerdictOf(conditionsByReactionId.get(reaction.id) ?? NO_CONDITIONS, {
                    kelvin: request.kelvin,
                    kilopascals: request.kilopascals,
                    inWater: inWater(reaction),
                    activationSource: reactionEnergeticsOf(reaction).activationSource
                })
            ]))
        });
    }
}

function sameVerdictRequest(first: VerdictRequest | null, second: VerdictRequest | null): boolean {
    if (first === null || second === null) {
        return first === second;
    }

    const secondIds = new Set(second.reactions.map((reaction) => reaction.id));

    return first.request.kelvin === second.request.kelvin
        && first.request.kilopascals === second.request.kilopascals
        && first.reactions.length === second.reactions.length
        && first.reactions.every((reaction) => secondIds.has(reaction.id));
}
