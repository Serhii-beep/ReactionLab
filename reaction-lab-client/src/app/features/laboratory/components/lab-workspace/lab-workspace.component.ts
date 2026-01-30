import { CommonModule } from "@angular/common";
import { AfterViewInit, Component, computed, ElementRef, inject, OnDestroy, signal, ViewChild } from "@angular/core";
import { Molecule3D, MoleculeFactoryService, SceneManagerService } from "../../../../three-engine";
import { ElementService, MoleculeService } from "../../../../core/services";
import { ElementSummary, Molecule, MoleculeSummary } from "../../../../core/models";
import { forkJoin, tap } from "rxjs";
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
    selector: 'app-lab-workspace',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './lab-workspace.component.html',
    styleUrls: [ './lab-workspace.component.scss' ]
})
export class LabWorkspaceComponent implements AfterViewInit, OnDestroy {
    @ViewChild('canvasContainer', { static: true }) canvasContainer!: ElementRef<HTMLDivElement>;

    private readonly sceneManager = inject(SceneManagerService);
    private readonly moleculeFactory = inject(MoleculeFactoryService);
    private readonly elementService = inject(ElementService);
    private readonly moleculeService = inject(MoleculeService);

    readonly loading = signal(true);
    readonly error = signal<string | null>(null);
    readonly availableMolecules = signal<MoleculeSummary[]>([]);

    private readonly sceneMolecules = signal<Molecule3D[]>([]);
    readonly sceneMoleculeCount = computed(() => this.sceneMolecules().length);

    private elements: ElementSummary[] = [];
    private moleculeCache = new Map<string, Molecule>();
    private moleculeSpawnOffset = 0;

    constructor() {
        this.loadData();
    }

    ngAfterViewInit(): void {
        this.sceneManager.initialize(this.canvasContainer);
    }

    ngOnDestroy(): void {
        this.clearWorkspace();
        this.sceneManager.dispose();
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

    clearWorkspace(): void {
        const molecules = this.sceneMolecules();

        molecules.forEach(molecule => {
            this.sceneManager.removeObject(molecule.id);
            this.moleculeFactory.disposeMolecule(molecule);
        });

        this.sceneMolecules.set([]);
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
            this.sceneManager.addObject(molecule3D.id, molecule3D.group);
        } else {
            console.warn(`Could not create 3D model for molecule: ${molecule.formula}`);
        }
    }

    private loadData(): void {
        this.loading.set(true);
        this.error.set(null);

        forkJoin({
            elements: this.elementService.getAll(),
            molecules: this.moleculeService.getAll()
        }).pipe(
            takeUntilDestroyed(),
            tap({
                next: ({ elements, molecules }) => {
                    this.elements = elements;
                    this.availableMolecules.set(molecules);
                    this.loading.set(false);
                },
                error: (err) => {
                    this.error.set('Failed to load data. Please check if the API is running');
                    this.loading.set(false);
                    console.error('Failed to load data', err);
                }
            })
        ).subscribe();
    }
}