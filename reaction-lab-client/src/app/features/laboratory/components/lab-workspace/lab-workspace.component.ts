import { CommonModule } from "@angular/common";
import { AfterViewInit, Component, computed, DestroyRef, effect, ElementRef, inject, OnDestroy, signal, ViewChild } from "@angular/core";
import { Atom3D, AtomFactoryService, Molecule3D, MoleculeFactoryService, SceneManagerService } from "../../../../three-engine";
import { ElementService, MoleculeService, ReactionDetectorService, ReactionExecutorService, ReactionService, SceneReactants } from "../../../../core/services";
import { ElementSummary, Molecule, MoleculeSummary, Reaction, ReactionSummary } from "../../../../core/models";
import { firstValueFrom, tap } from "rxjs";
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { PeriodicTablePanelComponent } from "../../../periodic-table";
import { MoleculesPanelComponent } from "../../../molecules-panel";
import { DropZoneDirective } from "../../../../shared";
import { DropEvent } from "../../../../shared/drag-drop/drag-drop.model";
import { SelectionService } from "../../../../core/services/selection.service";
import { ContextPanelComponent } from "../../../../shared/components/context-panel/context-panel.component";
import { MoleculeDetailPanelComponent } from "../molecule-detail-panel/molecule-detail-panel.component";
import * as THREE from 'three';
import { ElementDetailPanelComponent } from "../element-detail-panel/element-detail-panel.component";
import { ReactionsPanelComponent } from "../../../reactions-panel/components/reactions-panel/reactions-panel.component";

@Component({
    selector: 'app-lab-workspace',
    standalone: true,
    imports: [
        CommonModule,
        PeriodicTablePanelComponent,
        MoleculesPanelComponent,
        MatIconModule,
        MatButtonModule,
        MatTooltipModule,
        DropZoneDirective,
        ContextPanelComponent,
        MoleculeDetailPanelComponent,
        ElementDetailPanelComponent,
        ReactionsPanelComponent
    ],
    templateUrl: './lab-workspace.component.html',
    styleUrls: ['./lab-workspace.component.scss']
})
export class LabWorkspaceComponent implements AfterViewInit, OnDestroy {
    @ViewChild('canvasContainer', { static: true }) canvasContainer!: ElementRef<HTMLDivElement>;

    private readonly sceneManager = inject(SceneManagerService);
    private readonly moleculeFactory = inject(MoleculeFactoryService);
    private readonly elementService = inject(ElementService);
    private readonly moleculeService = inject(MoleculeService);
    private readonly selectionService = inject(SelectionService);
    private readonly atomFactory = inject(AtomFactoryService);
    private readonly reactionDetector = inject(ReactionDetectorService);
    private readonly reactionExecutor = inject(ReactionExecutorService);
    private readonly reactionService = inject(ReactionService);
    private readonly destroyRef = inject(DestroyRef);

    readonly loading = signal(true);
    readonly error = signal<string | null>(null);
    readonly isPanelCollapsed = signal(false);
    readonly isRightPanelCollapsed = signal(false);
    readonly isReactionsPanelCollapsed = signal(false);
    readonly isReactionExecuting = this.reactionExecutor.isExecuting;
    readonly reactionPhase = this.reactionExecutor.currentPhase;
    readonly reactionError = this.reactionExecutor.error;

    private readonly sceneMolecules = signal<Molecule3D[]>([]);
    private readonly sceneAtoms = signal<Atom3D[]>([]);
    readonly sceneMoleculeCount = computed(() => this.sceneMolecules().length);
    readonly sceneAtomCount = computed(() => this.sceneAtoms().length);
    readonly isDetailPanelOpen = computed(() => this.selectionService.hasMoleculeSelected());
    readonly isElementPanelOpen = computed(() => this.selectionService.hasElementSelected());
    readonly reactionPhaseLabel = computed(() => {
        switch (this.reactionPhase()) {
            case 'gathering': return 'Gathering reactants...';
            case 'breaking': return 'Breaking bonds...';
            case 'transforming': return 'Forming products...';
            case 'complete': return 'Reaction complete!';
            default: return 'Preparing reaction...';
        }
    });
    private readonly sceneContents = computed(() => {
        const molecules = this.sceneMolecules();
        const atoms = this.sceneAtoms();

        const moleculeIds = [...new Set(molecules.map(m => m.molecule?.id).filter((id): id is string => id !== undefined))];
        const elementIds = [...new Set(atoms.map(a => a.element.id).filter((id): id is string => id !== undefined))];

        return { moleculeIds, elementIds };
    });

    private elements: ElementSummary[] = [];
    private moleculeCache = new Map<string, Molecule>();
    private molecule3DMap = new Map<string, Molecule3D>();
    private atom3DMap = new Map<string, Atom3D>();
    private moleculeSpawnOffset = 0;

    readonly selectedMolecule3D = computed(() => {
        const selectedId = this.sceneManager.selectedObjectId();
        return selectedId ? this.molecule3DMap.get(selectedId) ?? null : null;
    });

    constructor() {
        this.loadData();

        effect(() => {
            const selectedId = this.sceneManager.selectedObjectId();

            if (selectedId) {
                const molecule3D = this.molecule3DMap.get(selectedId);
                if (molecule3D) {
                    this.selectionService.selectMolecule(selectedId, molecule3D);
                }

                const atom3D = this.atom3DMap.get(selectedId);
                if (atom3D) {
                    this.selectionService.selectElement(selectedId, atom3D);
                    return;
                }
            } else {
                this.selectionService.clearSelection();
            }
        });

        effect(() => {
            const selectedId = this.selectionService.selectedObjectId();

            this.molecule3DMap.forEach((mol, id) => {
                this.moleculeFactory.highlightMolecule(mol, id === selectedId);
            });

            this.atom3DMap.forEach((atom, id) => {
                this.atomFactory.highlightAtom(atom, id === selectedId);
            });
        });

        effect(() => {
            const contents = this.sceneContents();
            this.reactionDetector.updateSceneContents(contents);
        });
    }

    ngAfterViewInit(): void {
        this.sceneManager.initialize(this.canvasContainer);

        this.sceneManager.createFloatingActions(
            [{ id: 'delete', icon: 'delete', color: '#ff6b6b' }],
            (actionId) => {
                if (actionId === 'delete') {
                    this.deleteSelected();
                }
            }
        ).catch(err => console.error('Failed to create floating actions:', err));
    }

    ngOnDestroy(): void {
        this.clearWorkspace();
        this.sceneManager.dispose();
    }

    onPanelCollapsed(collapsed: boolean): void {
        this.isPanelCollapsed.set(collapsed);

        setTimeout(() => {
            this.sceneManager.resize();
        }, 350);
    }

    onRightPanelCollapsed(collapsed: boolean): void {
        this.isRightPanelCollapsed.set(collapsed);

        setTimeout(() => {
            this.sceneManager.resize();
        }, 350);
    }

    deleteSelected(): void {
        const selectedId = this.selectionService.selectedObjectId();
        if (!selectedId) {
            return;
        }

        const molecule3D = this.molecule3DMap.get(selectedId);
        if (molecule3D) {
            this.sceneManager.removeObject(selectedId);            
            this.moleculeFactory.disposeMolecule(molecule3D);
            this.molecule3DMap.delete(selectedId);
            this.sceneMolecules.update(molecules => molecules.filter(m => m.id !== selectedId));
            this.sceneManager.clearSelection();
            return;
        }

        const atom3D = this.atom3DMap.get(selectedId);
        if (atom3D) {
            this.sceneManager.removeObject(selectedId);
            this.atomFactory.disposeAtom(atom3D);
            this.atom3DMap.delete(selectedId);
            this.sceneAtoms.update(atoms => atoms.filter(a => a.id !== selectedId));
            this.sceneManager.clearSelection();
            return;
        }
    }

    addMoleculeToScene(moleculeSummary: MoleculeSummary): void {
        const cached = this.moleculeCache.get(moleculeSummary.id);

        if (cached) {
            this.createAndAddMolecule(cached);
        } else {
            this.moleculeService.getById(moleculeSummary.id).pipe(
                tap({
                    next: (molecule) => {
                        this.moleculeCache.set(molecule.id, molecule);
                        this.createAndAddMolecule(molecule);
                    },
                    error: (err) => {
                        console.error('Failed to load molecule:', err);
                    }
                })
            ).subscribe();
        }
    }

    closeDetailPanel(): void {
        this.sceneManager.clearSelection();
        this.selectionService.clearSelection();
    }

    onReactionPanelCollapsed(collapsed: boolean): void {
        this.isReactionsPanelCollapsed.set(collapsed);
    }

    async onExecuteReaction(reactionSummary: ReactionSummary): Promise<void> {
        if (this.reactionExecutor.isExecuting()) {
            return;
        }

        try {
            const reaction = await firstValueFrom(this.reactionService.getById(reactionSummary.id));

            const sceneReactants = this.gatherSceneReactants(reaction);

            this.sceneManager.clearSelection();
            this.selectionService.clearSelection();

            const result = await this.reactionExecutor.execute({
                reaction,
                sceneReactants,
                elements: this.elements,
                callbacks: {
                    addMoleculeToScene: (mol) => this.addMolecule3DToScene(mol),
                    addAtomToScene: (atom) => this.addAtom3DToScene(atom),
                    removeMoleculeFromScene: (id) => this.removeMolecule3DFromScene(id),
                    removeAtomFromScene: (id) => this.removeAtom3DFromScene(id)
                }
            });

            if (!result.success) {
                console.error('Reaction failed:', result.error);
            }
        } catch (error) {
            console.error('Failed to execute reaction:', error);
        }
    }

    stopReaction(): void {
        this.reactionExecutor.stop();
    }

    private addMoleculeToSceneAtPosition(moleculeSummary: MoleculeSummary, position: { x: number; y: number; z: number }): void {
        const cached = this.moleculeCache.get(moleculeSummary.id);

        if (cached) {
            this.createAndAddMoleculeAtPosition(cached, position);
        } else {
            this.moleculeService.getById(moleculeSummary.id).pipe(
                tap({
                    next: (molecule) => {
                        this.moleculeCache.set(molecule.id, molecule);
                        this.createAndAddMoleculeAtPosition(molecule, position);
                    },
                    error: (err) => {
                        console.error('Failed to load molecule:', err);
                    }
                })
            ).subscribe();
        }
    }

    private createAndAddMoleculeAtPosition(molecule: Molecule, position: { x: number; y: number; z: number }): void {
        const molecule3D = this.moleculeFactory.createFromMolecule(molecule, this.elements);

        if (molecule3D) {
            molecule3D.group.position.set(position.x, position.y, position.z);

            this.sceneMolecules.update(molecules => [...molecules, molecule3D]);
            this.molecule3DMap.set(molecule3D.id, molecule3D);
            this.sceneManager.addObject(molecule3D.id, molecule3D.group);
        } else {
            console.error(`Could not create 3D model for molecule: ${molecule.formula}`);
        }
    }

    onDrop(event: DropEvent): void {
        if (this.reactionExecutor.isExecuting()) {
            return;
        }

        if (event.data.type === 'molecule') {
            this.onMoleculeDrop(event);
        } else if (event.data.type === 'element') {
            this.onElementDrop(event);
        }
    }

    onMoleculeDrop(event: DropEvent<unknown>): void {
        const molecule = event.data.data as MoleculeSummary;
        const worldPosition = this.sceneManager.screenToWorldPosition(event.dropPosition.x, event.dropPosition.y);
        const safePosition = this.sceneManager.findNonOverlappingPosition(worldPosition);
        this.addMoleculeToSceneAtPosition(molecule, safePosition);
    }

    onElementDrop(event: DropEvent): void {
        const element = event.data.data as ElementSummary;
        const worldPosition = this.sceneManager.screenToWorldPosition(event.dropPosition.x, event.dropPosition.y);
        const safePosition = this.sceneManager.findNonOverlappingPosition(worldPosition, 1);
        this.createAtomAtPosition(element, safePosition);
    }

    clearWorkspace(): void {
        const molecules = this.sceneMolecules();

        molecules.forEach(molecule => {
            this.sceneManager.removeObject(molecule.id);
            this.moleculeFactory.disposeMolecule(molecule);
        });

        this.sceneMolecules.set([]);
        this.molecule3DMap.clear();

        const atoms = this.sceneAtoms();
        atoms.forEach(atom => {
            this.sceneManager.removeObject(atom.id);
            this.atomFactory.disposeAtom(atom);
        });
        this.sceneAtoms.set([]);
        this.atom3DMap.clear();

        this.sceneManager.clearSelection();
        this.selectionService.clearSelection();
        this.reactionDetector.reset();
        this.moleculeSpawnOffset = 0;
    }

    resetCamera(): void {
        this.sceneManager.resetCamera();
    }

    retry(): void {
        this.loadData();
    }

    private createAtomAtPosition(element: ElementSummary, position: { x: number, y: number, z: number}): void {
        const atom3D = this.atomFactory.createAtom(element, new THREE.Vector3(position.x, position.y, position.z));
        this.sceneAtoms.update(atoms => [...atoms, atom3D]);
        this.atom3DMap.set(atom3D.id, atom3D);
        this.sceneManager.addObject(atom3D.id, atom3D.group);
    }

    private createAndAddMolecule(molecule: Molecule): void {
        const molecule3D = this.moleculeFactory.createFromMolecule(molecule, this.elements);

        if (molecule3D) {
            const col = this.moleculeSpawnOffset % 4;
            const row = Math.floor(this.moleculeSpawnOffset / 4);
            const offsetX = (col - 1.5) * 5;
            const offsetZ = row * 5;

            molecule3D.group.position.set(offsetX, 0, offsetZ);
            this.moleculeSpawnOffset++;

            this.sceneMolecules.update(molecules => [...molecules, molecule3D]);
            this.molecule3DMap.set(molecule3D.id, molecule3D);
            this.sceneManager.addObject(molecule3D.id, molecule3D.group);
        } else {
            console.warn(`Could not create 3D model for molecule: ${molecule.formula}`);
        }
    }

    private gatherSceneReactants(reaction: Reaction): SceneReactants {
        const molecules = new Map<string, Molecule3D[]>();
        const atoms = new Map<string, Atom3D[]>();

        for (const participant of reaction.reactants) {
            if (participant.moleculeId) {
                const matching = this.sceneMolecules().filter(m => m.molecule?.id === participant.moleculeId);

                if (matching.length > 0) {
                    molecules.set(participant.moleculeId, [...matching]);
                }
            } else if (participant.elementId) {
                const matching = this.sceneAtoms().filter(a => a.element.id === participant.elementId);

                if (matching.length > 0) {
                    atoms.set(participant.elementId, [...matching]);
                }
            }
        }

        return { molecules, atoms };
    }

    private addMolecule3DToScene(molecule3D: Molecule3D): void {
        this.sceneMolecules.update(molecules => [...molecules, molecule3D]);
        this.molecule3DMap.set(molecule3D.id, molecule3D);
        this.sceneManager.addObject(molecule3D.id, molecule3D.group);
    }

    private addAtom3DToScene(atom3D: Atom3D): void {
        this.sceneAtoms.update(atoms => [...atoms, atom3D]);
        this.atom3DMap.set(atom3D.id, atom3D);
        this.sceneManager.addObject(atom3D.id, atom3D.group);
    }

    private removeMolecule3DFromScene(id: string): void {
        const molecule3D = this.molecule3DMap.get(id);
        if (molecule3D) {
            this.sceneManager.removeObject(id);
            this.moleculeFactory.disposeMolecule(molecule3D);
            this.molecule3DMap.delete(id);
            this.sceneMolecules.update(molecules => molecules.filter(m => m.id !== id));
        }
    }

    private removeAtom3DFromScene(id: string): void {
        const atom3D = this.atom3DMap.get(id);
        if (atom3D) {
            this.sceneManager.removeObject(id);
            this.atomFactory.disposeAtom(atom3D);
            this.atom3DMap.delete(id);
            this.sceneAtoms.update(atoms => atoms.filter(a => a.id !== id));
        }
    }

    private loadData(): void {
        this.loading.set(true);
        this.error.set(null);

        this.elementService.getAll().pipe(
            takeUntilDestroyed(this.destroyRef),
            tap({
                next: (elements) => {
                    this.elements = elements;
                    this.loading.set(false);
                },
                error: (err) => {
                    this.error.set('Failed to load data');
                    this.loading.set(false);
                }
            })
        ).subscribe();
    }
}