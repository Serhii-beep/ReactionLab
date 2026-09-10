import { afterNextRender, afterRenderEffect, ChangeDetectionStrategy, Component, computed, DOCUMENT, effect, ElementRef, inject, untracked } from "@angular/core";
import { provideEngine } from "../../../engine/engine-providers";
import { Box3, Color } from "three";
import { Theme } from "../../../core/theme/theme";
import { WorkspaceStore } from "../../../state/workspace-store";
import { ElementsClient } from "../../../data/elements/elements-client";
import { SubstanceDetailsClient } from "../../../data/substances/substance-details-client";
import { EngineContext } from "../../../engine/core/engine-context";
import { RenderLoop } from "../../../engine/core/render-loop";
import { BenchStage } from "../../../engine/scene/bench-stage";
import { AtomRenderer } from "../../../engine/objects/atom-renderer";
import { AtomLabels } from "../../../engine/objects/atom-labels";
import { buildBenchUnits } from "./bench-units";
import { ViewportObserver } from "../../../engine/core/viewport-observer";
import { LOOKS } from "../../../engine/rendering/look";
import { tokenColor } from "../../../engine/core/css-color";
import { layoutBench, PlacedAtom } from "../../../engine/scene/bench-layout";
import { lodFor } from "../../../engine/resources/geometry-cache";
import { projectedRadius } from "../../../engine/core/projection";
import { BondRenderer } from "../../../engine/objects/bond-renderer";
import { SceneViewport } from "./scene-viewport";
import { CameraController } from "../../../engine/interaction/camera-controller";
import { framingFor } from "../../../engine/core/camera-framing";
import { prefersReducedMotion } from "../../../core/platform/reduced-motion";

const LIGHT_INK = new Color(0xffffff);
const REFERENCE_HEIGHT = 900;

@Component({
    selector: 'app-scene-canvas',
    template: '',
    styleUrl: './scene-canvas.scss',
    providers: [provideEngine()],
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        '(dblclick)': 'viewport.requestFit()'
    }
})
export class SceneCanvas {
    protected readonly viewport = inject(SceneViewport);

    private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
    private readonly view = inject(DOCUMENT).defaultView ?? window;
    private readonly theme = inject(Theme);
    private readonly workspace = inject(WorkspaceStore);
    private readonly elements = inject(ElementsClient);
    private readonly details = inject(SubstanceDetailsClient);
    private readonly context = inject(EngineContext);
    private readonly loop = inject(RenderLoop);
    private readonly camera = inject(CameraController);
    private readonly stage = inject(BenchStage);
    private readonly atoms = inject(AtomRenderer);
    private readonly bonds = inject(BondRenderer);
    private readonly labels = inject(AtomLabels);

    private readonly units = computed(() =>
        buildBenchUnits(this.workspace.entries(), this.details.loaded(), this.elements.all.value()));

    private bounds = new Box3();
    private started = false;

    constructor() {
        inject(ViewportObserver).onResize(() => this.frame(false));

        this.context.scene.add(this.atoms.root, this.bonds.root, this.labels.root);

        effect(() => {
            for (const entry of this.workspace.entries()) {
                untracked(() => this.details.ensure(entry.substance.id));
            }
        });

        effect(() => this.rebuild());

        effect(() => {
            this.viewport.fitRequests();
            untracked(() => this.frame(true));
        })
        
        afterRenderEffect(() => {
            const look = LOOKS[this.theme.resolved()];

            this.stage.applyLook(look, (token) => tokenColor(this.host.nativeElement, token, this.view));
        });

        afterNextRender(() => {
            this.host.nativeElement.append(this.context.canvas);
            this.loop.onRender((delta) => this.camera.update(delta));
            this.loop.onRender(() => this.labels.update(this.context.camera, this.context.canvas.clientHeight));
            this.loop.start();
            this.started = true;
        });
    }

    private rebuild(): void {
        const { atoms, bonds, bounds } = layoutBench(this.units());
        const height = this.context.canvas.clientHeight || REFERENCE_HEIGHT;

        this.bounds = bounds;

        const distance = this.frame(this.started);

        this.stage.fit(distance, bounds);
        const lod = lodFor(projectedRadius(smallestRadius(atoms), distance, this.context.camera.fov, height));

        this.atoms.render(atoms, lod);
        this.bonds.render(bonds, lod);
        this.labels.render(atoms, bonds, { dark: tokenColor(this.host.nativeElement, '--cat-ink', this.view), light: LIGHT_INK });
    }

    private frame(transition: boolean): number {
        const framing = framingFor(this.context.camera, this.bounds);

        this.camera.frame(framing.center, framing.distance, this.bounds, transition && !prefersReducedMotion(this.view));

        return framing.distance;
    }
}

function smallestRadius(atoms: readonly PlacedAtom[]): number {
    let smallest = Infinity;

    for (const atom of atoms) {
        smallest = Math.min(smallest, atom.radius);
    }

    return smallest;
}