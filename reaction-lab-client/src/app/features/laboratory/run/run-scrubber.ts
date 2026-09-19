import { ChangeDetectionStrategy, Component, computed, ElementRef, inject, input, output, signal, viewChild } from "@angular/core";
import { phaseAt, ReactionTimeline } from "../../../data/reactions/reaction-phases";
import { TranslocoService } from "@jsverse/transloco";

interface HoveredPhase {
    readonly fraction: number;
    readonly label: string;
}

const BOUNDARY_TOLERANCE = 0.001;

@Component({
    selector: 'app-run-scrubber',
    templateUrl: './run-scrubber.html',
    styleUrl: './run-scrubber.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class RunScrubber {
    readonly timeline = input.required<ReactionTimeline>();
    readonly elapsedSeconds = input.required<number>();
    readonly disabled = input(false);

    readonly seekRequested = output<number>();
    readonly scrubStarted = output<void>();
    readonly scrubEnded = output<void>();

    private readonly transloco = inject(TranslocoService);
    private readonly track = viewChild.required<ElementRef<HTMLInputElement>>('track');

    protected readonly hoverPhase = signal<HoveredPhase | null>(null);
    protected readonly fill = computed(() => `${(100 * this.elapsedSeconds()) / this.timeline().durationSeconds}%`);
    protected readonly ticks = computed(() =>
        this.timeline().phases.slice(1).map((phase) => `${(100 * phase.startSeconds) / this.timeline().durationSeconds}%`));
    protected readonly label = computed(() => this.transloco.translate('lab.run.scrubber'));
    protected readonly valueText = computed(() => this.transloco.translate('lab.run.position', {
        phase: this.phaseLabel(this.elapsedSeconds()),
        elapsed: this.elapsedSeconds().toFixed(1),
        duration: this.timeline().durationSeconds.toFixed(1)
    }));

    protected onInput(event: Event): void {
        this.seekRequested.emit((event.target as HTMLInputElement).valueAsNumber);
    }

    protected onKeydown(event: KeyboardEvent): void {
        if (event.key !== 'ArrowLeft' && event.key !== 'ArrowRight') {
            return;
        }

        event.preventDefault();
        this.seekRequested.emit(adjacentBoundary(this.timeline(), this.elapsedSeconds(), event.key === 'ArrowRight'));
    }

    protected onPointerMove(event: PointerEvent): void {
        const rect = this.track().nativeElement.getBoundingClientRect();
        const fraction = Math.min(Math.max((event.clientX - rect.left) / rect.width, 0), 1);

        this.hoverPhase.set({ fraction, label: this.phaseLabel(fraction * this.timeline().durationSeconds) });
    }

    protected onPointerLeave(): void {
        this.hoverPhase.set(null);
    }

    private phaseLabel(seconds: number): string {
        return this.transloco.translate(`lab.run.phases.${phaseAt(this.timeline(), seconds).name}`);
    }
}

function adjacentBoundary(timeline: ReactionTimeline, seconds: number, forward: boolean): number {
    const boundaries = [0, ...timeline.phases.map((phase) => phase.endSeconds)];

    if (forward) {
        return boundaries.find((boundary) => boundary > seconds + BOUNDARY_TOLERANCE) ?? timeline.durationSeconds;
    }

    return [...boundaries].reverse().find((boundary) => boundary < seconds - BOUNDARY_TOLERANCE) ?? 0;
}