import { Box3, Color, Sphere, Vector3 } from "three";
import { Disposable } from "../core/disposal-scope";
import { EngineContext } from "../core/engine-context";
import { CameraController } from "../interaction/camera-controller";
import { HighlightFade } from "../interaction/highlight-fade";
import { PickingService } from "../interaction/picking-service";
import { AtomLabels, LabelInk } from "../objects/atom-labels";
import { AtomRenderer } from "../objects/atom-renderer";
import { BondRenderer } from "../objects/bond-renderer";
import { SelectionOutline } from "../objects/selection-outline";
import { layoutBench, LayoutUnit, PlacedAtom, PlacedBond, smallestRadiusOf } from "./bench-layout";
import { BenchStage } from "./bench-stage";
import { Lod } from "../resources/geometry-cache";
import { projectedRadius, worldPerPixel } from "../core/projection";
import { distanceFor, framingFor } from "../core/camera-framing";
import { LodController } from "../performance/lod-controller";
import { ReactionDirector } from "../animation/reaction-director";
import { ReactionMotion } from "../animation/reaction-motion";
import { StagedBench } from "../animation/unit-staging";
import { ReactionScript } from "../animation/reaction-script";
import { ReactionEffects } from "../particles/reaction-effects";
import { QualityLevel } from "../performance/quality-governor";
import { RunCamera } from "../cinematography/run-camera";

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
    readonly director: ReactionDirector;
    readonly effects: ReactionEffects;
    readonly runCamera: RunCamera;
}

export interface UnitAnchor {
    readonly centerX: number;
    readonly centerY: number;
    readonly radiusPixels: number;
    readonly viewportWidth: number;
    readonly viewportHeight: number;
    readonly onScreen: boolean;
}

const OUTLINE_PIXELS = 2.5;
const FOCUS_MARGIN = 2.4;
const OFFSET_MARGIN = 1.5;
const REFERENCE_HEIGHT = 900;
const REFERENCE_WIDTH = 1600;

export class BenchScene implements Disposable {
    private atoms: readonly PlacedAtom[] = [];
    private bonds: readonly PlacedBond[] = [];
    private bounds = new Box3();
    private sphereByUnitId: ReadonlyMap<string, Sphere> = new Map();
    private motion: ReactionMotion | null = null;
    private runTailSeconds = 0;
    private ink: LabelInk | null = null;
    private viewportWidth = REFERENCE_WIDTH;
    private viewportHeight = REFERENCE_HEIGHT;
    private lodInUse: Lod = 'high';
    private cameraWasMoving = false;
    private needsRender = true;
    private outlineDirty = true;

    private readonly projectedCenter = new Vector3();

    constructor(private readonly collaborators: BenchSceneCollaborators) {
        const { context, atoms, bonds, outline, labels, effects } = collaborators;

        context.scene.add(atoms.root, bonds.root, outline.root, labels.root, effects.root);
    }

    setViewportSize(width: number, height: number): void {
        if (width > 0 && height > 0) {
            this.viewportWidth = width;
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
        
        this.motion = null;
        this.bounds = layout.bounds;
        this.fitBench(layout.atoms, animated);
        this.show(layout);
    }

    beginRun(script: ReactionScript, animated: boolean): void {
        const motion = new ReactionMotion(script, layoutBench(script.unitsBefore), layoutBench(script.unitsAfter));
        const opening = motion.frameAt(0);
        const { effects, runCamera } = this.collaborators;

        this.motion = motion;
        this.runTailSeconds = 0;
        effects.begin(script.emissions, motion.emissionAnchors);
        this.bounds = motion.bounds;
        runCamera.end();

        if (animated) {
            runCamera.begin(script, motion.cameraCues);
        }

        this.fitBench(opening.atoms, animated);
        this.show(opening);
    }

    endRun(): void {
        this.motion = null;
        this.collaborators.effects.end();
        this.collaborators.runCamera.end();
    }

    setHighlight(targetLevels: ReadonlyMap<string, number>): void {
        this.collaborators.highlight.setTargetLevels(targetLevels);
    }

    pick(clientX: number, clientY: number): PlacedAtom | null {
        return this.collaborators.picking.pick(this.atoms, clientX, clientY);
    }

    frame(animated: boolean): number {
        const { context, camera, stage, director, runCamera } = this.collaborators;
        const framing = framingFor(context.camera, this.bounds);

        if (runCamera.directing && !animated) {
            camera.fenceTo(this.bounds, framing.center);
            runCamera.follow(director.elapsedSeconds);
        } else {
            camera.frame(framing.center, framing.distance, this.bounds, animated);
        }

        stage.fit(framing.distance, this.bounds);
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

    anchorFor(unitId: string): UnitAnchor | null {
        const sphere = this.sphereByUnitId.get(unitId);

        if (!sphere) {
            return null;
        }

        const camera = this.collaborators.context.camera;
        const ndc = this.projectedCenter.copy(sphere.center).project(camera);

        return {
            centerX: ((ndc.x + 1) / 2) * this.viewportWidth,
            centerY: ((1 - ndc.y) / 2) * this.viewportHeight,
            radiusPixels: projectedRadius(sphere.radius, camera.position.distanceTo(sphere.center), camera.fov, this.viewportHeight),
            viewportWidth: this.viewportWidth,
            viewportHeight: this.viewportHeight,
            onScreen: ndc.z < 1 && Math.abs(ndc.x) <= OFFSET_MARGIN && Math.abs(ndc.y) <= OFFSET_MARGIN
        };
    }

    refreshLod(): void {
        const { context, camera, atoms, bonds, lod, runCamera } = this.collaborators;
        const chosen = lod.choose(smallestRadiusOf(this.atoms), runCamera.directing ? runCamera.nearestDistance : camera.distance, context.camera.getEffectiveFOV(), this.viewportHeight);

        if (chosen === this.lodInUse) {
            return;
        }

        this.lodInUse = chosen;
        atoms.render(this.atoms, chosen);
        bonds.render(this.bonds, chosen);
        this.needsRender = true;
    }

    setEffectsQuality(level: QualityLevel): void {
        this.collaborators.effects.setQuality(level);
    }

    update(deltaSeconds: number): boolean {
        const { context, camera, highlight, outline, labels } = this.collaborators;

        this.advanceRun(deltaSeconds);

        const moved = camera.update(deltaSeconds);

        if (moved) {
            context.camera.updateMatrixWorld();
        }

        const fading = highlight.update(deltaSeconds);

        this.settleLod(moved);

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
        const { context, atoms, bonds, outline, labels, effects } = this.collaborators;

        context.scene.remove(atoms.root, bonds.root, outline.root, labels.root, effects.root);
    }

    private fitBench(atoms: readonly PlacedAtom[], animated: boolean): void {
        const { context, lod, runCamera } = this.collaborators;
        const distance = this.frame(animated && !runCamera.directing);

        this.lodInUse = lod.choose(smallestRadiusOf(atoms), runCamera.directing ? runCamera.nearestDistance : distance, context.camera.getEffectiveFOV(), this.viewportHeight);
    }

    private show(staged: StagedBench): void {
        const { atoms, bonds, labels, outline } = this.collaborators;
        const differentAtoms = staged.atoms !== this.atoms;

        this.atoms = staged.atoms;
        this.bonds = staged.bonds;
        this.sphereByUnitId = staged.sphereByUnitId;
        atoms.render(this.atoms, this.lodInUse);
        bonds.render(this.bonds, this.lodInUse);
        outline.render(this.atoms, this.bonds);
        this.outlineDirty = true;
        this.needsRender = true;

        if (differentAtoms && this.ink) {
            labels.render(this.atoms, this.bonds, this.ink);
        } else if (this.motion !== null) {
            labels.refreshSticks(this.bonds);
        }
    }

    private advanceRun(deltaSeconds: number): void {
        const { director, effects, runCamera } = this.collaborators;

        if (this.motion === null) {
            return;
        }

        if (director.consumeMovement()) {
            this.show(this.motion.frameAt(director.elapsedSeconds));
            runCamera.follow(director.elapsedSeconds);
            this.runTailSeconds = 0;
        }

        this.runTailSeconds += director.status === 'finished' ? deltaSeconds : 0;

        if (effects.setElapsed(director.elapsedSeconds + this.runTailSeconds)) {
            this.needsRender = true;
        }
    }

    private settleLod(moved: boolean) {
        if (this.cameraWasMoving && !moved) {
            this.refreshLod();
        }

        this.cameraWasMoving = moved;
    }
}