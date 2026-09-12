import { afterNextRender, afterRenderEffect, ChangeDetectionStrategy, Component, computed, DestroyRef, DOCUMENT, effect, ElementRef, inject, isDevMode, signal, untracked } from "@angular/core";
import { provideEngine } from "../../../engine/engine-providers";
import { Color } from "three";
import { Theme } from "../../../core/theme/theme";
import { WorkspaceStore } from "../../../state/workspace-store";
import { ElementsClient } from "../../../data/elements/elements-client";
import { SubstanceDetailsClient } from "../../../data/substances/substance-details-client";
import { EngineContext } from "../../../engine/core/engine-context";
import { RenderLoop } from "../../../engine/core/render-loop";
import { BenchStage } from "../../../engine/scene/bench-stage";
import { buildBenchUnits } from "./bench-units";
import { ViewportObserver } from "../../../engine/core/viewport-observer";
import { LOOKS } from "../../../engine/rendering/look";
import { tokenColor } from "../../../engine/core/css-color";
import { PlacedAtom } from "../../../engine/scene/bench-layout";
import { SceneViewport } from "./scene-viewport";
import { SelectionStore } from "../../../state/selection-store";
import { PointerInput } from "../../../engine/interaction/pointer-input";
import { BenchScene } from "../../../engine/scene/bench-scene";
import { ActivatedRoute } from "@angular/router";
import { QualityGovernor } from "../../../engine/performance/quality-governor";
import { ContextGuard } from "../../../engine/core/context-guard";
import { NotificationService } from "../../../core/notifications/notification-service";
import { TranslocoService } from "@jsverse/transloco";
import { SceneStats } from "./scene-stats";

type HighlightLevelsByUnitId = ReadonlyMap<string, number>;

const LIGHT_INK = new Color(0xffffff);
const HOVER = 0.6;
const SELECTED = 1;

@Component({
    selector: 'app-scene-canvas',
    template: '@if (showStats) { <app-scene-stats /> }',
    styleUrl: './scene-canvas.scss',
    providers: [provideEngine()],
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        '[class.scene-hovering]': 'hovered() !== null'
    },
    imports: [SceneStats]
})
export class SceneCanvas {
    protected readonly hovered = signal<PlacedAtom | null>(null);
    protected readonly showStats = isDevMode() && inject(ActivatedRoute).snapshot.queryParamMap.has('stats');

    private readonly viewport = inject(SceneViewport);
    private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
    private readonly view = inject(DOCUMENT).defaultView ?? window;
    private readonly theme = inject(Theme);
    private readonly workspace = inject(WorkspaceStore);
    private readonly selection = inject(SelectionStore);
    private readonly elements = inject(ElementsClient);
    private readonly details = inject(SubstanceDetailsClient);
    private readonly context = inject(EngineContext);
    private readonly loop = inject(RenderLoop);
    private readonly stage = inject(BenchStage);
    private readonly scene = inject(BenchScene);
    private readonly pointer = inject(PointerInput);
    private readonly governor = inject(QualityGovernor);
    private readonly guard = inject(ContextGuard);
    private readonly notifications = inject(NotificationService);
    private readonly transloco = inject(TranslocoService);
    private readonly reducedMotion = this.view.matchMedia('(prefers-reduced-motion: reduce)');

    private readonly units = computed(() =>
        buildBenchUnits(this.workspace.entries(), this.details.loaded(), this.elements.all.value()));

    private readonly highlightLevels = computed<HighlightLevelsByUnitId>(() => {
        const selected = this.selection.selectedId();
        const hovered = this.hovered();
        const highlightLevels = new Map<string, number>();

        for (const unit of this.units()) {
            if (unit.substanceId === selected) {
                highlightLevels.set(unit.id, SELECTED);
            }
        }

        if (hovered && !highlightLevels.has(hovered.unitId)) {
            highlightLevels.set(hovered.unitId, HOVER);
        }

        return highlightLevels;
    }, { equal: sameLevels });

    private started = false;
    private graphicsNotice: number | null = null;

    constructor() {
        inject(ViewportObserver).onResize((_, height) => {
            this.scene.setViewportHeight(height);
            this.scene.frame(false);
        });

        inject(DestroyRef).onDestroy(this.pointer.bind({
            move: (x, y) => this.hovered.set(this.scene.pick(x, y)),
            leave: () => this.hovered.set(null),
            click: (x, y) => this.select(this.scene.pick(x, y)),
            doubleClick: (x, y) => this.focus(this.scene.pick(x, y))
        }));

        effect(() => {
            for (const entry of this.workspace.entries()) {
                untracked(() => this.details.ensure(entry.substance.id));
            }
        });

        effect(() => {
            const units = this.units();

            untracked(() => {
                this.scene.setUnits(units, this.started && this.animated());
                this.repick();
            });
        });

        effect(() => {
            const highlightLevels = this.highlightLevels();

            untracked(() => this.scene.setHighlight(highlightLevels));
        });

        effect(() => {
            this.viewport.fitRequests();
            untracked(() => this.scene.frame(this.animated()));
        })
        
        afterRenderEffect(() => this.applyTheme());

        afterNextRender(() => this.start());
    }

    private start(): void {
        this.host.nativeElement.append(this.context.canvas);
        this.scene.setReducedMotion(!this.animated());
        this.loop.onRender((deltaSeconds) => this.scene.update(deltaSeconds));
        this.loop.onPresent((intervalSeconds) => this.governor.sample(intervalSeconds));
        this.governor.onChange(() => {
            this.scene.refreshLod();
            this.loop.invalidate();
        });
        this.guard.onLost(() => this.graphicsLost());
        this.guard.onRestored(() => this.graphicsRestored());
        this.governor.apply(0);
        this.loop.start();
        this.started = true;
    }

    private applyTheme(): void {
        const look = LOOKS[this.theme.resolved()];
        const resolve = (token: string) => tokenColor(this.host.nativeElement, token, this.view);

        this.stage.applyLook(look, resolve);
        this.scene.setAccent(resolve('--accent'));
        this.scene.setLabelInk({ dark: resolve('--cat-ink'), light: LIGHT_INK });

        this.loop.invalidate();
    }

    private animated(): boolean {
        return !this.reducedMotion.matches;
    }

    private select(atom: PlacedAtom | null): void {
        if (atom) {
            this.selection.toggle(atom.substanceId);
        } else {
            this.selection.clear();
        }
    }

    private focus(atom: PlacedAtom | null): void {
        if (!atom) {
            this.viewport.requestFit();

            return;
        }

        if (!this.selection.isSelected(atom.substanceId)) {
            this.selection.toggle(atom.substanceId);
        }

        this.scene.focusUnit(atom.unitId, this.animated());
    }

    private repick(): void {
        const last = this.pointer.lastPosition;

        this.hovered.set(last ? this.scene.pick(last.x, last.y) : null);
    }

    private graphicsLost(): void {
        this.graphicsNotice = this.notifications.show(
            'warning',
            this.transloco.translate('lab.graphics.lost'),
            this.transloco.translate('lab.graphics.lostDetail'),
            true
        );
    }

    private graphicsRestored(): void {
        if (this.graphicsNotice !== null) {
            this.notifications.dismiss(this.graphicsNotice);
            this.graphicsNotice = null;
        }

        this.notifications.show('success', this.transloco.translate('lab.graphics.restored'));
    }
}

function sameLevels(a: HighlightLevelsByUnitId, b: HighlightLevelsByUnitId): boolean {
    if (a.size !== b.size) {
        return false;
    }

    for (const [id, level] of a) {
        if (b.get(id) !== level) {
            return false;
        }
    }

    return true;
}