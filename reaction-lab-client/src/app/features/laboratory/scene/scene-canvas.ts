import { afterNextRender, afterRenderEffect, ChangeDetectionStrategy, Component, DOCUMENT, ElementRef, inject } from "@angular/core";
import { provideEngine } from "../../../engine/engine-providers";
import { Theme } from "../../../core/theme/theme";
import { EngineContext } from "../../../engine/core/engine-context";
import { RenderLoop } from "../../../engine/core/render-loop";
import { Ground } from "../../../engine/objects/ground";
import { ViewportObserver } from "../../../engine/core/viewport-observer";
import { tokenColor } from "../../../engine/core/css-color";

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
    private readonly ground = new Ground();

    constructor() {
        inject(ViewportObserver);

        this.context.scene.add(this.ground);

        afterRenderEffect(() => {
            this.theme.resolved();
            this.ground.setColor(tokenColor(this.host.nativeElement, '--surface-base', this.view));
        });

        afterNextRender(() => {
            this.host.nativeElement.append(this.context.canvas);
            this.loop.start();
        });
    }
}