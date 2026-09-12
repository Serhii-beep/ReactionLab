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
import { BondRenderer } from "./objects/bond-renderer";
import { CameraController } from "./interaction/camera-controller";
import { PickingService } from "./interaction/picking-service";
import { PointerInput } from "./interaction/pointer-input";
import { SelectionOutline } from "./objects/selection-outline";
import { HighlightFade } from "./interaction/highlight-fade";
import { BenchScene } from "./scene/bench-scene";
import { PostProcessingPipeline } from "./rendering/post-processing-pipeline";

const HIGHLIGHT_RISE = 0.2;
const HIGHLIGHT_FALL = 0.3;

export function provideEngine(): Provider[] {
    return [...coreProviders(), ...sceneProviders()];
}

function coreProviders(): Provider[] {
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
        owned(PostProcessingPipeline, () => new PostProcessingPipeline(inject(EngineContext))),
        owned(ViewportObserver, () => new ViewportObserver(
            hostElement(),
            inject(EngineContext),
            inject(DOCUMENT).defaultView ?? window)),
        owned(RenderLoop, () => {
            const loop = new RenderLoop(inject(EngineContext), inject(DOCUMENT));

            inject(ViewportObserver).onResize((width, height) => loop.suspend(width === 0 || height === 0));

            return loop;
        }),
        owned(CameraController, () => new CameraController(inject(EngineContext), hostElement())),
        owned(PointerInput, () => new PointerInput(hostElement())),
        owned(GeometryCache, () => new GeometryCache()),
        owned(MaterialCache, () => new MaterialCache()),
        owned(LabelAtlas, () => new LabelAtlas(inject(EngineContext))),
        { provide: HighlightFade, useFactory: () => new HighlightFade(HIGHLIGHT_RISE, HIGHLIGHT_FALL) },
        { provide: PickingService, useFactory: () => new PickingService(inject(EngineContext)) },
        owned(BenchScene, () => new BenchScene({
            context: inject(EngineContext),
            camera: inject(CameraController),
            stage: inject(BenchStage),
            atoms: inject(AtomRenderer),
            bonds: inject(BondRenderer),
            labels: inject(AtomLabels),
            outline: inject(SelectionOutline),
            highlight: inject(HighlightFade),
            picking: inject(PickingService)
        }))
    ];
}

function sceneProviders(): Provider[] {
    return [
        owned(BenchStage, () => new BenchStage(inject(EngineContext), inject(PostProcessingPipeline))),
        owned(AtomRenderer, () => new AtomRenderer(inject(GeometryCache), inject(MaterialCache))),
        owned(BondRenderer, () => new BondRenderer(inject(GeometryCache), inject(MaterialCache))),
        owned(AtomLabels, () => new AtomLabels(inject(LabelAtlas))),
        owned(SelectionOutline, () => new SelectionOutline(inject(GeometryCache))),
    ];
}

function owned<T extends Disposable>(token: ProviderToken<T>, create: () => T): Provider {
    return { provide: token, useFactory: () => inject(DisposalScope).add(create()) };
}

function hostElement(): HTMLElement {
    return inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
}