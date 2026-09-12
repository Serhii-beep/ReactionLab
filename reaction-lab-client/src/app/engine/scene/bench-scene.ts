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
import { Lod } from "../resources/geometry-cache";
import { worldPerPixel } from "../core/projection";
import { distanceFor, framingFor } from "../core/camera-framing";
import { LodController } from "../performance/lod-controller";

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
    readonly lod: LodController;
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
    private lodInUse: Lod = 'high';
    private cameraWasMoving = false;
    private needsRender = true;
    private outlineDirty = true;

    constructor(private readonly collaborators: BenchSceneCollaborators) {
        const { context, atoms, bonds, outline, labels } = collaborators;

        context.scene.add(atoms.root, bonds.root, outline.root, labels.root);
    }

    setViewportHeight(height: number): void {
        if (height > 0) {
            this.viewportHeight = height;
            this.needsRender = true;
        }
    }

    setReducedMotion(enabled: boolean): void {
        this.collaborators.highlight.setReducedMotion(enabled);
    }

    setAccent(color: Color): void {
        this.collaborators.outline.setAccent(color);
        this.needsRender = true;
    }

    setLabelInk(ink: LabelInk): void {
        this.ink = ink;
        this.collaborators.labels.render(this.atoms, this.bonds, ink);
        this.needsRender = true;
    }

    setUnits(units: readonly LayoutUnit[], animated: boolean): void {
        const layout = layoutBench(units);
        const { context, stage, atoms, bonds, labels, outline, lod } = this.collaborators;

        this.atoms = layout.atoms;
        this.bonds = layout.bonds;
        this.bounds = layout.bounds;
        this.sphereByUnitId = layout.units;

        const distance = this.frame(animated);

        this.lodInUse = lod.choose(smallestRadius(this.atoms), distance, context.camera.fov, this.viewportHeight);
        stage.fit(distance, this.bounds);
        atoms.render(this.atoms, this.lodInUse);
        bonds.render(this.bonds, this.lodInUse);
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

    frame(animated: boolean): number {
        const { context, camera } = this.collaborators;
        const framing = framingFor(context.camera, this.bounds);

        camera.frame(framing.center, framing.distance, this.bounds, animated);
        this.needsRender = true;

        return framing.distance;
    }

    focusUnit(unitId: string, animated: boolean): boolean {
        const sphere = this.sphereByUnitId.get(unitId);

        if (!sphere) {
            return false;
        }

        const { context, camera } = this.collaborators;

        camera.focus(sphere.center, distanceFor(context.camera, sphere.radius, FOCUS_MARGIN), animated);
        this.needsRender = true;

        return true;
    }

    refreshLod(): void {
        const { context, camera, atoms, bonds, lod } = this.collaborators;
        const chosen = lod.choose(smallestRadius(this.atoms), camera.distance, context.camera.fov, this.viewportHeight);

        if (chosen === this.lodInUse) {
            return;
        }

        this.lodInUse = chosen;
        atoms.render(this.atoms, chosen);
        bonds.render(this.bonds, chosen);
        this.needsRender = true;
    }

    update(deltaSeconds: number): boolean {
        const { context, camera, highlight, outline, labels } = this.collaborators;
        const moved = camera.update(deltaSeconds);
        const fading = highlight.update(deltaSeconds);

        if (this.cameraWasMoving && !moved) {
            this.refreshLod();
        }

        this.cameraWasMoving = moved;

        if (moved || fading || this.outlineDirty) {
            outline.update(highlight.highlightLevels, OUTLINE_PIXELS * worldPerPixel(camera.distance, context.camera.fov, this.viewportHeight));
            this.outlineDirty = false;
        }

        if (moved || this.needsRender) {
            labels.update(context.camera, this.viewportHeight);
        }

        const present = moved || fading || this.needsRender;

        this.needsRender = false;

        return present;
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