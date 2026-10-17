import { ChangeDetectionStrategy, Component, computed, DestroyRef, DOCUMENT, effect, ElementRef, inject, signal, untracked, viewChild } from "@angular/core";
import { Vector3 } from "three";
import { BenchScene } from "../../../engine/scene/bench-scene";
import { BenchLayout, layoutBench, PlacedAtom } from "../../../engine/scene/bench-layout";
import { PickingService } from "../../../engine/interaction/picking-service";
import { PointerInput } from "../../../engine/interaction/pointer-input";
import { EngineContext } from "../../../engine/core/engine-context";
import { RenderLoop } from "../../../engine/core/render-loop";
import { LensShift } from "../../../engine/core/lens-shift";
import { SymmetryGuides } from "../../../engine/objects/symmetry-guides";
import { tokenColor } from "../../../engine/core/css-color";
import { projectedRadius } from "../../../engine/core/projection";
import { formatCharge } from "../../../design-system/chemistry/chem-formula-parts";
import { ElementsClient } from "../../../data/elements/elements-client";
import { SceneViewport } from "../scene/scene-viewport";
import { SketchEditor } from "./sketch-editor";
import { SketchAnalysis } from "./sketch-analysis";
import { MoleculeBuilder } from "./molecule-builder";
import { PlacedSketch } from "./sketch-geometry";
import { atomPickId, openValencePickId, pickTargetOf, sketchUnit, SketchPickTarget } from "./sketch-units";
import { indexOfKey, KeyedSelection, keyedSelectionOf } from "./sketch-selection";
import { symmetryGuidesFor } from "./symmetry-guide-set";

type SceneTarget = SketchPickTarget | KeyedSelection;

interface ScreenAnchor {
    readonly center: Vector3;
    readonly radius: number;
}

interface HostSize {
    readonly width: number;
    readonly height: number;
}

const BOND_REACH = 0.18;
const BOND_ANCHOR_RADIUS = 0.22;
const RING_GAP = 4;

@Component({
    selector: 'app-builder-scene',
    templateUrl: './builder-scene.html',
    styleUrl: './builder-scene.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: { 'aria-hidden': 'true' }
})
export class BuilderScene {
    private readonly editor = inject(SketchEditor);
    private readonly analysis = inject(SketchAnalysis);
    private readonly builder = inject(MoleculeBuilder);
    private readonly elements = inject(ElementsClient);
    private readonly viewport = inject(SceneViewport);
    private readonly scene = inject(BenchScene);
    private readonly picking = inject(PickingService);
    private readonly context = inject(EngineContext);
    private readonly loop = inject(RenderLoop);
    private readonly lensShift = inject(LensShift);
    private readonly guides = inject(SymmetryGuides);
    private readonly host = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
    private readonly view = inject(DOCUMENT).defaultView ?? window;
    private readonly reducedMotion = this.view.matchMedia('(prefers-reduced-motion: reduce)');
    private readonly selectionRing = viewChild.required<ElementRef<HTMLElement>>('selectionRing');
    private readonly anchorRing = viewChild.required<ElementRef<HTMLElement>>('anchorRing');
    private readonly hoverRing = viewChild.required<ElementRef<HTMLElement>>('hoverRing');
    private readonly hovered = signal<SceneTarget | null>(null);
    private readonly projected = new Vector3();

    private layout: BenchLayout = layoutBench([]);
    private atomsByPickId = new Map<string, PlacedAtom>();

    protected readonly charges = computed(() => {
        const placed = this.analysis.placed();

        return placed === null ? [] : placed.description.atoms
            .map((atom, index) => ({
                pickId: atomPickId(placed.sketch.atoms[index].key),
                text: formatCharge(atom.formalCharge),
                inferred: atom.formalChargeInferred
            }))
            .filter((badge) => badge.text !== '');
    });

    constructor() {
        const destroyRef = inject(DestroyRef);

        destroyRef.onDestroy(inject(PointerInput).bind({
            move: (x, y) => this.hovered.set(this.targetAt(x, y)),
            leave: () => this.hovered.set(null),
            click: (x, y, additive) => this.activate(this.targetAt(x, y), additive),
            doubleClick: () => this.scene.frame(this.animated())
        }));
        destroyRef.onDestroy(this.loop.onRender(() => this.placeOverlay()));
        destroyRef.onDestroy(() => this.guides.hide());

        effect(() => {
            const placed = this.analysis.placed();
            const elements = this.elements.all.value();

            untracked(() => this.show(placed, new Map(elements.map((element) => [element.symbol, element]))));
        });

        effect(() => {
            const visible = this.builder.showSymmetry() && this.builder.complete();
            const placed = this.analysis.placed();

            untracked(() => this.showSymmetry(visible ? placed : null));
        });

        effect(() => {
            const coveredPixels = this.viewport.benchCoveredPixels();

            untracked(() => {
                this.lensShift.cover(coveredPixels, this.animated());
                this.scene.frame(this.animated());
            });
        });
    }

    private show(placed: PlacedSketch | null, elements: Parameters<typeof sketchUnit>[1]): void {
        const units = placed === null || placed.sketch.atoms.length === 0
            ? []
            : [sketchUnit(placed, elements, tokenColor(this.host, '--text-tertiary', this.view))];

        this.layout = layoutBench(units);
        this.atomsByPickId = new Map(this.layout.atoms.flatMap((atom) => (atom.pickId === undefined ? [] : [[atom.pickId, atom] as const])));
        this.scene.setUnits(units, this.animated());
        this.showSymmetry(this.builder.showSymmetry() && this.builder.complete() ? placed : null);
    }

    private showSymmetry(placed: PlacedSketch | null): void {
        const guides = placed === null ? null : symmetryGuidesFor(placed, this.layout);

        if (guides === null) {
            this.guides.hide();
        } else {
            this.guides.setColor(tokenColor(this.host, '--accent', this.view));
            this.guides.show(guides);
        }

        this.loop.invalidate();
    }

    private targetAt(clientX: number, clientY: number): SceneTarget | null {
        const atomTarget = pickTargetOf(this.scene.pick(clientX, clientY)?.pickId);

        if (atomTarget !== null) {
            return atomTarget;
        }

        const bond = this.picking.pickBond(this.layout.bonds, clientX, clientY, BOND_REACH);
        const from = pickTargetOf(bond?.from.pickId);
        const to = pickTargetOf(bond?.to.pickId);

        return from?.kind === 'atom' && to?.kind === 'atom' ? { kind: 'bond', fromKey: from.key, toKey: to.key } : null;
    }

    private activate(target: SceneTarget | null, additive: boolean): void {
        const sketch = this.editor.sketch();

        if (target === null) {
            this.editor.clearSelection();
        } else if (target.kind === 'atom') {
            this.whenPresent([indexOfKey(sketch, target.key)], ([atom]) => this.editor.pickAtom(atom, additive));
        } else if (target.kind === 'openValence') {
            this.whenPresent([this.analysis.upToDate() ? indexOfKey(sketch, target.ownerKey) : -1], ([owner]) =>
                this.editor.attachTo(owner, this.builder.element()));
        } else {
            this.whenPresent([indexOfKey(sketch, target.fromKey), indexOfKey(sketch, target.toKey)], ([from, to]) =>
                this.editor.pickBond(from, to));
        }
    }

    private whenPresent(indices: readonly number[], act: (indices: readonly number[]) => void): void {
        if (indices.every((index) => index >= 0)) {
            act(indices);
        }
    }

    private placeOverlay(): boolean {
        const size: HostSize = { width: this.host.clientWidth, height: this.host.clientHeight };
        const sketch = this.editor.sketch();
        const selection = keyedSelectionOf(this.editor.selection(), sketch);
        const connectingFrom = this.editor.connectingFrom();
        const anchor = connectingFrom === null ? null : keyedSelectionOf({ kind: 'atom', atom: connectingFrom }, sketch);
        const hovered = this.hovered();

        this.placeRing(this.selectionRing().nativeElement, selection === null ? null : this.anchorOf(selection), size);
        this.placeRing(this.anchorRing().nativeElement, anchor === null ? null : this.anchorOf(anchor), size);
        this.placeRing(this.hoverRing().nativeElement, hovered === null ? null : this.anchorOf(hovered), size);

        for (const badge of this.host.querySelectorAll<HTMLElement>('[data-pick-id]')) {
            const atom = this.atomsByPickId.get(badge.dataset['pickId'] ?? '');
            const point = atom === undefined ? null : this.screenPointOf({ center: atom.position, radius: atom.radius }, size);

            badge.style.visibility = point === null ? 'hidden' : 'visible';

            if (point !== null) {
                badge.style.transform = `translate(${Math.round(point.x + point.radius * 0.7)}px, ${Math.round(point.y - point.radius * 1.3)}px)`;
            }
        }

        return false;
    }

    private placeRing(ring: HTMLElement, anchor: ScreenAnchor | null, size: HostSize): void {
        const point = anchor === null ? null : this.screenPointOf(anchor, size);

        ring.style.visibility = point === null ? 'hidden' : 'visible';

        if (point !== null) {
            const diameter = Math.round(2 * (point.radius + RING_GAP));

            ring.style.width = `${diameter}px`;
            ring.style.height = `${diameter}px`;
            ring.style.transform = `translate(${Math.round(point.x - diameter / 2)}px, ${Math.round(point.y - diameter / 2)}px)`;
        }
    }

    private anchorOf(target: SceneTarget): ScreenAnchor | null {
        if (target.kind === 'bond') {
            const from = this.atomsByPickId.get(atomPickId(target.fromKey));
            const to = this.atomsByPickId.get(atomPickId(target.toKey));

            return from && to ? { center: from.position.clone().lerp(to.position, 0.5), radius: BOND_ANCHOR_RADIUS } : null;
        }

        const atom = this.atomsByPickId.get(target.kind === 'atom' ? atomPickId(target.key) : openValencePickId(target.ownerKey, target.stub));

        return atom ? { center: atom.position, radius: atom.radius } : null;
    }

    private screenPointOf(anchor: ScreenAnchor, size: HostSize): { readonly x: number; readonly y: number; readonly radius: number } | null {
        const camera = this.context.camera;
        const ndc = this.projected.copy(anchor.center).project(camera);

        if (ndc.z > 1 || size.width === 0 || size.height === 0) {
            return null;
        }

        return {
            x: ((ndc.x + 1) / 2) * size.width,
            y: ((1 - ndc.y) / 2) * size.height,
            radius: projectedRadius(anchor.radius, camera.position.distanceTo(anchor.center), camera.fov, size.height)
        };
    }

    private animated(): boolean {
        return !this.reducedMotion.matches;
    }
}
