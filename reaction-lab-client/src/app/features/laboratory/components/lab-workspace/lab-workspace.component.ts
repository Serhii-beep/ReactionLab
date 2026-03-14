import { CommonModule } from "@angular/common";
import { AfterViewInit, Component, computed, effect, ElementRef, inject, OnDestroy, signal, ViewChild } from "@angular/core";
import { Molecule3D, MoleculeFactoryService, SceneManagerService } from "../../../../three-engine";
import { ElementService, MoleculeService } from "../../../../core/services";
import { ElementSummary, Molecule, MoleculeSummary } from "../../../../core/models";
import { tap } from "rxjs";
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
        MoleculeDetailPanelComponent
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

    readonly loading = signal(true);
    readonly error = signal<string | null>(null);
    readonly isPanelCollapsed = signal(false);
    readonly isRightPanelCollapsed = signal(false);

    private readonly sceneMolecules = signal<Molecule3D[]>([]);
    readonly sceneMoleculeCount = computed(() => this.sceneMolecules().length);
    readonly isDetailPanelOpen = computed(() => this.selectionService.hasMoleculeSelected());

    private elements: ElementSummary[] = [];
    private moleculeCache = new Map<string, Molecule>();
    private molecule3DMap = new Map<string, Molecule3D>();
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
            } else {
                this.selectionService.clearSelection();
            }
        });

        effect(() => {
            const selectedId = this.selectionService.selectedObjectId();

            this.molecule3DMap.forEach((mol, id) => {
                this.moleculeFactory.highlightMolecule(mol, id === selectedId);
            });
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
        if (!molecule3D) {
            return;
        }

        this.sceneManager.removeObject(selectedId);
        this.sceneManager.clearSelection();
        this.moleculeFactory.disposeMolecule(molecule3D);
        this.molecule3DMap.delete(selectedId);
        this.sceneMolecules.update(molecules => molecules.filter(m => m.id !== selectedId))
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

    onMoleculeDrop(event: DropEvent<unknown>): void {
        const molecule = event.data.data as MoleculeSummary;
        const worldPosition = this.sceneManager.screenToWorldPosition(event.dropPosition.x, event.dropPosition.y);
        const safePosition = this.sceneManager.findNonOverlappingPosition(worldPosition);
        this.addMoleculeToSceneAtPosition(molecule, safePosition);
    }

    clearWorkspace(): void {
        const molecules = this.sceneMolecules();

        molecules.forEach(molecule => {
            this.sceneManager.removeObject(molecule.id);
            this.moleculeFactory.disposeMolecule(molecule);
        });

        this.sceneMolecules.set([]);
        this.molecule3DMap.clear();
        this.sceneManager.clearSelection();
        this.selectionService.clearSelection();
        this.moleculeSpawnOffset = 0;
    }

    resetCamera(): void {
        this.sceneManager.resetCamera();
    }

    retry(): void {
        this.loadData();
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

    private loadData(): void {
        this.loading.set(true);
        this.error.set(null);

        this.elementService.getAll().pipe(
            takeUntilDestroyed(),
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