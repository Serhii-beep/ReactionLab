import { DestroyRef, DOCUMENT, ElementRef, inject, Provider, ProviderToken } from "@angular/core";
import { Disposable, DisposalScope } from "./core/disposal-scope";
import { EngineContext } from "./core/engine-context";
import { ViewportObserver } from "./core/viewport-observer";
import { RenderLoop } from "./core/render-loop";
import { BenchStage } from "./scene/bench-stage";
import { GeometryCache } from "./resources/geometry-cache";
import { MaterialCache } from "./resources/material-cache";
import { LabelAtlas } from "./resources/label-atlas";
import { AtomRenderer } from "./objects/atom-renderer";
import { AtomLabels } from "./objects/atom-labels";

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
        owned(EngineContext, () => new EngineContext()),
        owned(ViewportObserver, () => new ViewportObserver(
            inject<ElementRef<HTMLElement>>(ElementRef).nativeElement,
            inject(EngineContext),
            inject(DOCUMENT).defaultView ?? window)),
        owned(RenderLoop, () => {
            const loop = new RenderLoop(inject(EngineContext), inject(DOCUMENT));

            inject(ViewportObserver).onResize((width, height) => loop.suspend(width === 0 || height === 0));

            return loop;
        }),
        owned(BenchStage, () => new BenchStage(inject(EngineContext))),
        owned(GeometryCache, () => new GeometryCache()),
        owned(MaterialCache, () => new MaterialCache()),
        owned(LabelAtlas, () => new LabelAtlas(inject(EngineContext))),
        owned(AtomRenderer, () => new AtomRenderer(inject(GeometryCache), inject(MaterialCache))),
        owned(AtomLabels, () => new AtomLabels(inject(LabelAtlas)))
    ];
}

function owned<T extends Disposable>(token: ProviderToken<T>, create: () => T): Provider {
    return { provide: token, useFactory: () => inject(DisposalScope).add(create()) };
}