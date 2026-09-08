import { afterNextRender, afterRenderEffect, ChangeDetectionStrategy, Component, DOCUMENT, effect, ElementRef, inject, isDevMode } from "@angular/core";
import { provideEngine } from "../../../engine/engine-providers";
import { Theme } from "../../../core/theme/theme";
import { EngineContext } from "../../../engine/core/engine-context";
import { RenderLoop } from "../../../engine/core/render-loop";
import { ViewportObserver } from "../../../engine/core/viewport-observer";
import { tokenColor } from "../../../engine/core/css-color";
import { BenchStage } from "../../../engine/scene/bench-stage";
import { ActivatedRoute } from "@angular/router";
import { DisposalScope } from "../../../engine/core/disposal-scope";
import { CalibrationProbe } from "../../../engine/objects/calibration-probe";
import { LOOKS } from "../../../engine/rendering/look";
import { ElementsClient } from "../../../data/elements/elements-client";
import { GeometryCache } from "../../../engine/resources/geometry-cache";
import { MaterialCache } from "../../../engine/resources/material-cache";
import { LabelAtlas } from "../../../engine/resources/label-atlas";
import { Color } from "three";

const PROBE_SYMBOLS = ['H', 'C', 'N', 'O', 'S', 'Cl', 'Na', 'Fe'];

@Component({
    selector: 'app-scene-canvas',
    template: '',
    styleUrl: './scene-canvas.scss',
    providers: [provideEngine()],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class SceneCanvas {
    private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
    private readonly view = inject(DOCUMENT).defaultView ?? window;
    private readonly theme = inject(Theme);
    private readonly elements = inject(ElementsClient);
    private readonly scope = inject(DisposalScope);
    private readonly context = inject(EngineContext);
    private readonly loop = inject(RenderLoop);
    private readonly stage = inject(BenchStage);
    private readonly geometries = inject(GeometryCache);
    private readonly materials = inject(MaterialCache);
    private readonly labels = inject(LabelAtlas);
    private probe: CalibrationProbe | null = null;

    constructor() {
        inject(ViewportObserver);

        if (isDevMode() && inject(ActivatedRoute).snapshot.queryParamMap.has('probe')) {
            effect(() => this.buildProbe());
        }

        afterRenderEffect(() => {
            const look = LOOKS[this.theme.resolved()];

            this.stage.applyLook(look, (token) => tokenColor(this.host.nativeElement, token, this.view));
        });

        afterNextRender(() => {
            this.host.nativeElement.append(this.context.canvas);
            this.loop.start();
        });
    }

    private buildProbe(): void {
        const elements = this.elements.all.value();

        if (this.probe || elements.length === 0) {
            return;
        }

        const samples = PROBE_SYMBOLS.flatMap((symbol) => {
            const element = elements.find((candidate) => candidate.symbol === symbol);

            return element ? [{ symbol, color: new Color(element.displayColor) }] : [];
        });

        const labelColor = tokenColor(this.host.nativeElement, '--text-primary', this.view);

        this.probe = this.scope.add(new CalibrationProbe(samples, this.geometries, this.materials, this.labels, labelColor));
        this.context.scene.add(this.probe);
    }
}