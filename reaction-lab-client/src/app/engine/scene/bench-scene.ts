import { Box3, Color, Sphere } from "three";
import { Disposable } from "../core/disposal-scope";
import { EngineContext } from "../core/engine-context";
import { CameraController } from "../interaction/camera-controller";
import { HighlightFade } from "../interaction/highlight-fade";
import { PickingService } from "../interaction/picking-service";
import { AtomLabels, LabelInk } from "../objects/atom-labels";
import { AtomRenderer } from "../objects/atom-renderer";
import { BondRenderer } from "../objects/bond-renderer";
import { SelectionOutline } from "../objects/selection-outline";
import { layoutBench, LayoutUnit, PlacedAtom, PlacedBond } from "./bench-layout";
import { BenchStage } from "./bench-stage";
import { lodFor } from "../resources/geometry-cache";
import { projectedRadius, worldPerPixel } from "../core/projection";
import { distanceFor, framingFor } from "../core/camera-framing";

export interface BenchSceneCollaborators {
    readonly context: EngineContext;
    readonly camera: CameraController;
    readonly stage: BenchStage;
    readonly atoms: AtomRenderer;
    readonly bonds: BondRenderer;
    readonly labels: AtomLabels;
    readonly outline: SelectionOutline;
    readonly highlight: HighlightFade;
    readonly picking: PickingService;
}

const OUTLINE_PIXELS = 2.5;
const FOCUS_MARGIN = 2.4;
const REFERENCE_HEIGHT = 900;

export class BenchScene implements Disposable {
    private atoms: readonly PlacedAtom[] = [];
    private bonds: readonly PlacedBond[] = [];
    private bounds = new Box3();
    private sphereByUnitId: ReadonlyMap<string, Sphere> = new Map();
    private ink: LabelInk | null = null;
    private viewportHeight = REFERENCE_HEIGHT;
    private outlineDirty = true;

    constructor(private readonly collaborators: BenchSceneCollaborators) {
        const { context, atoms, bonds, outline, labels } = collaborators;

        context.scene.add(atoms.root, bonds.root, outline.root, labels.root);
    }

    setViewportHeight(height: number): void {
        if (height > 0) {
            this.viewportHeight = height;
        }
    }

    setReducedMotion(enabled: boolean): void {
        this.collaborators.highlight.setReducedMotion(enabled);
    }

    setAccent(color: Color): void {
        this.collaborators.outline.setAccent(color);
    }

    setLabelInk(ink: LabelInk): void {
        this.ink = ink;
        this.collaborators.labels.render(this.atoms, this.bonds, ink);
    }

    setUnits(units: readonly LayoutUnit[], transition: boolean): void {
        const layout = layoutBench(units);
        const { context, stage, atoms, bonds, labels, outline } = this.collaborators;

        this.atoms = layout.atoms;
        this.bonds = layout.bonds;
        this.bounds = layout.bounds;
        this.sphereByUnitId = layout.units;

        const distance = this.frame(transition);
        const lod = lodFor(projectedRadius(smallestRadius(this.atoms), distance, context.camera.fov, this.viewportHeight));

        stage.fit(distance, this.bounds);
        atoms.render(this.atoms, lod);
        bonds.render(this.bonds, lod);
        outline.render(this.atoms, this.bonds);
        this.outlineDirty = true;

        if (this.ink) {
            labels.render(this.atoms, this.bonds, this.ink);
        }
    }

    setHighlight(targetLevels: ReadonlyMap<string, number>): void {
        this.collaborators.highlight.setTargetLevels(targetLevels);
    }

    pick(clientX: number, clientY: number): PlacedAtom | null {
        return this.collaborators.picking.pick(this.atoms, clientX, clientY);
    }

    frame(transition: boolean): number {
        const { context, camera } = this.collaborators;
        const framing = framingFor(context.camera, this.bounds);

        camera.frame(framing.center, framing.distance, this.bounds, transition);

        return framing.distance;
    }

    focusUnit(unitId: string, transition: boolean): boolean {
        const sphere = this.sphereByUnitId.get(unitId);

        if (!sphere) {
            return false;
        }

        this.collaborators.camera.focus(sphere.center, distanceFor(this.collaborators.context.camera, sphere.radius, FOCUS_MARGIN), transition);

        return true;
    }

    update(delta: number): void {
        const { context, camera, highlight, outline, labels } = this.collaborators;
        const moved = camera.update(delta);
        const fading = highlight.update(delta);

        if (moved || fading || this.outlineDirty) {
            outline.update(highlight.highlightLevels, OUTLINE_PIXELS * worldPerPixel(camera.distance, context.camera.fov, this.viewportHeight));
            this.outlineDirty = false;
        }

        labels.update(context.camera, this.viewportHeight);
    }

    dispose(): void {
        const { context, atoms, bonds, outline, labels } = this.collaborators;

        context.scene.remove(atoms.root, bonds.root, outline.root, labels.root);
    }
}

function smallestRadius(atoms: readonly PlacedAtom[]): number {
    let smallest = Infinity;

    for (const atom of atoms) {
        smallest = Math.min(smallest, atom.radius);
    }

    return smallest;
}