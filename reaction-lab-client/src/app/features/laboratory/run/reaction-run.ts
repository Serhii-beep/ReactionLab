import { computed, DOCUMENT, effect, inject, Injectable, signal, untracked } from "@angular/core";
import { ReactionScript } from "../../../engine/animation/reaction-script";
import { ReactionSummary } from "../../../data/reactions/reaction";
import { activationBarrierOf, DEFAULT_RUN_SECONDS, phaseAt, reactionTimeline, ReactionTimeline, runSecondsOf } from "../../../data/reactions/reaction-phases";
import { WorkspaceItem, WorkspaceStore } from "../../../state/workspace-store";
import { UiStore } from "../../../state/ui-store";
import { SubstanceDetailsClient } from "../../../data/substances/substance-details-client";
import { ElementsClient } from "../../../data/elements/elements-client";
import { NotificationService } from "../../../core/notifications/notification-service";
import { LiveAnnouncer } from "@angular/cdk/a11y";
import { TranslocoService } from "@jsverse/transloco";
import { outcomeOf, productIdsOf, ReactionOutcome } from "../../../data/reactions/reaction-outcome";
import { buildReactionScript } from "./reaction-script-builder";
import { readiness } from "../../../data/reactions/reaction-readiness";
import { prefersReducedMotion } from "../../../core/platform/reduced-motion";
import { tap } from "rxjs";
import { choreographyTuningFor } from "./choreography-tuning";

export type ReactionRunStatus = 'idle' | 'preparing' | 'playing' | 'paused' | 'finished';
export type ReactionRunStep = 'before' | 'during' | 'after';

export interface ReactionRunDriver {
    start(script: ReactionScript): void;
    pause(): void;
    resume(): void;
    seek(seconds: number): void;
    stop(): void;
}

@Injectable()
export class ReactionRun {
    readonly status = signal<ReactionRunStatus>('idle');
    readonly reaction = signal<ReactionSummary | null>(null);
    readonly timeline = signal<ReactionTimeline>(reactionTimeline(DEFAULT_RUN_SECONDS));
    readonly elapsedSeconds = signal(0);
    readonly stepped = signal(false);
    readonly step = signal<ReactionRunStep>('before');

    readonly active = computed(() => this.status() !== 'idle');
    readonly currentPhase = computed(() => phaseAt(this.timeline(), this.elapsedSeconds()));

    private readonly workspace = inject(WorkspaceStore);
    private readonly ui = inject(UiStore);
    private readonly details = inject(SubstanceDetailsClient);
    private readonly elements = inject(ElementsClient);
    private readonly notifications = inject(NotificationService);
    private readonly announcer = inject(LiveAnnouncer);
    private readonly transloco = inject(TranslocoService);
    private readonly view = inject(DOCUMENT).defaultView ?? window;

    private driver: ReactionRunDriver | null = null;
    private script: ReactionScript | null = null;
    private outcome: ReactionOutcome | null = null;
    private committed = false;
    private entriesAfterCommit: readonly WorkspaceItem[] | null = null;
    private resumeAfterScrub = false;

    constructor() {
        effect(() => {
            const entries = this.workspace.entries();

            untracked(() => this.onBenchChanged(entries));
        })
    }

    attach(driver: ReactionRunDriver): () => void {
        this.driver = driver;

        return () => {
            if (this.driver === driver) {
                this.driver = null;
            }
        };
    }

    start(reaction: ReactionSummary): void {
        if (this.status() !== 'idle' || !readiness(reaction, this.workspace.counts()).runnable) {
            return;
        }

        this.ui.dismiss();
        this.reaction.set(reaction);
        this.status.set('preparing');
        this.elapsedSeconds.set(0);
        this.stepped.set(prefersReducedMotion(this.view));
        this.details.detailsOf(productIdsOf(reaction)).pipe(
            tap({
                next: (products) => this.begin(reaction, outcomeOf(reaction, products)),
                error: () => this.fail(reaction)
            })
        ).subscribe();
    }

    togglePause(): void {
        if (this.status() === 'playing') {
            this.pause();
        } else if (this.status() === 'paused') {
            this.resume();
        }
    }

    pause(): void {
        if (this.status() === 'playing') {
            this.driver?.pause();
            this.status.set('paused');
        }
    }

    resume(): void {
        if (this.status() === 'paused') {
            this.driver?.resume();
            this.status.set('playing');
        }
    }

    restart(): void {
        this.seek(0);
        this.resume();
    }

    seek(seconds: number): void {
        if (this.script === null) {
            return;
        }

        this.driver?.seek(seconds);
        this.elapsedSeconds.set(Math.min(Math.max(seconds, 0), this.timeline().durationSeconds));

        if (this.status() === 'finished' && seconds < this.timeline().durationSeconds) {
            this.status.set('paused');
        }
    }

    beginScrub(): void {
        this.resumeAfterScrub = this.status() === 'playing';
        this.pause();
    }

    endScrub(): void {
        if (this.resumeAfterScrub) {
            this.resumeAfterScrub = false;
            this.resume();
        }
    }

    stepTo(step: ReactionRunStep): void {
        const timeline = this.timeline();

        this.step.set(step);

        if (step === 'before') {
            this.seek(0);
        } else if (step === 'during') {
            this.seek(timeline.byName.transitionState.endSeconds);
        } else {
            this.seek(timeline.durationSeconds);
        }
    }

    replay(): void {
        if (this.status() === 'finished' && this.script !== null) {
            this.play(this.script);
        }
    }

    stop(): void {
        if (this.status() === 'idle') {
            return;
        }

        this.driver?.stop();
        this.reset();
    }

    tick(elapsedSeconds: number): void {
        if (this.status() === 'playing' || this.status() === 'paused') {
            this.elapsedSeconds.set(elapsedSeconds);
        }
    }

    markFinished(): void {
        if (this.status() === 'idle' || this.status() === 'preparing') {
            return;
        }

        this.status.set('finished');
        this.elapsedSeconds.set(this.timeline().durationSeconds);
        this.commitOnce();
    }

    private begin(reaction: ReactionSummary, outcome: ReactionOutcome): void {
        if (this.status() !== 'preparing' || this.reaction() !== reaction) {
            return;
        }

        const timeline = reactionTimeline(runSecondsOf(reaction), activationBarrierOf(reaction));
        const script = buildReactionScript(timeline, choreographyTuningFor(reaction), {
            entriesBefore: this.workspace.entries(),
            entriesAfter: this.workspace.entriesAfter(outcome),
            details: this.details.loaded(),
            elements: this.elements.all.value()
        });

        this.script = script;
        this.outcome = outcome;
        this.committed = false;
        this.timeline.set(timeline);
        this.play(script);
        this.announce('lab.announce.runStarted', reaction.name);
    }

    private play(script: ReactionScript): void {
        this.driver?.start(script);
        this.elapsedSeconds.set(0);

        if (this.stepped()) {
            this.driver?.pause();
            this.step.set('before');
            this.status.set('paused');
        } else {
            this.status.set('playing');
        }
    }

    private commitOnce(): void {
        const reaction = this.reaction();

        if (this.committed || this.outcome === null || reaction === null) {
            return;
        }

        this.committed = true;
        this.workspace.applyOutcome(this.outcome);
        this.entriesAfterCommit = this.workspace.entries();
        this.announce('lab.announce.runFinished', reaction.name);
    }

    private onBenchChanged(entries: readonly WorkspaceItem[]): void {
        if (this.status() !== 'idle' && entries !== this.entriesAfterCommit) {
            this.stop();
        }
    }

    private fail(reaction: ReactionSummary): void {
        if (this.status() === 'preparing' && this.reaction() === reaction) {
            this.reset();
            this.notifications.show('danger', this.transloco.translate('lab.run.failed'), this.transloco.translate('lab.run.failedDetail'));
        }
    }

    private reset(): void {
        this.status.set('idle');
        this.reaction.set(null);
        this.elapsedSeconds.set(0);
        this.step.set('before');
        this.script = null;
        this.outcome = null;
        this.committed = false;
        this.entriesAfterCommit = null;
        this.resumeAfterScrub = false;
    }

    private announce(key: string, name: string): void {
        void this.announcer.announce(this.transloco.translate(key, { name }), 'polite');
    }
}