import { afterNextRender, afterRenderEffect, ChangeDetectionStrategy, Component, DOCUMENT, ElementRef, inject, isDevMode } from "@angular/core";
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
    private readonly context = inject(EngineContext);
    private readonly loop = inject(RenderLoop);
    private readonly stage = inject(BenchStage);

    constructor() {
        inject(ViewportObserver);

        if (isDevMode() && inject(ActivatedRoute).snapshot.queryParamMap.has('probe')) {
            this.context.scene.add(inject(DisposalScope).add(new CalibrationProbe()));
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
}