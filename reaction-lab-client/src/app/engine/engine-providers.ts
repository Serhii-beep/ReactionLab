import { DestroyRef, DOCUMENT, ElementRef, inject, Provider } from "@angular/core";
import { DisposalScope } from "./core/disposal-scope";
import { EngineContext } from "./core/engine-context";
import { ViewportObserver } from "./core/viewport-observer";
import { RenderLoop } from "./core/render-loop";
import { BenchStage } from "./scene/bench-stage";

export function provideEngine(): Provider[] {
    return [
        {
            provide: DisposalScope,
            useFactory: () => {
                const scope = new DisposalScope();

                inject(DestroyRef).onDestroy(() => scope.dispose());

                return scope;
            }
        },
        {
            provide: EngineContext,
            useFactory: () => inject(DisposalScope).add(new EngineContext())
        },
        {
            provide: ViewportObserver,
            useFactory: () => {
                const host = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
                const view = inject(DOCUMENT).defaultView ?? window;

                return inject(DisposalScope).add(new ViewportObserver(host, inject(EngineContext), view));
            }
        },
        {
            provide: RenderLoop,
            useFactory: () => {
                const loop = inject(DisposalScope).add(new RenderLoop(inject(EngineContext), inject(DOCUMENT)));

                inject(ViewportObserver).onResize((width, height) => loop.suspend(width === 0 || height === 0));

                return loop;
            }
        },
        {
            provide: BenchStage,
            useFactory: () => inject(DisposalScope).add(new BenchStage(inject(EngineContext)))
        }
    ];
}