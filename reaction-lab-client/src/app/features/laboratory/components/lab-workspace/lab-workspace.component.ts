import { CommonModule } from "@angular/common";
import { AfterViewInit, Component, ElementRef, inject, OnDestroy, signal, ViewChild } from "@angular/core";
import { Molecule3D, MoleculeFactoryService, SceneManagerService } from "../../../../three-engine";
import { ElementService } from "../../../../core/services";
import { ElementSummary } from "../../../../core/models";
import { tap } from "rxjs";
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

    readonly loading = signal(true);
    readonly availableMolecules = ['H2O', 'CO2', 'CH4', 'O2', 'H2'];

    private elements: ElementSummary[] = [];
    private molecules: Molecule3D[] = [];
    private moleculeSpawnOffset = 0;

    constructor() {
        this.elementService.getAll().pipe(
            takeUntilDestroyed(),
            tap({
                next: (elements) => {
                    this.elements = elements;
                    this.loading.set(false);
                },
                error: () => {
                    this.loading.set(false);
                }
            })
        ).subscribe();
    }

    ngAfterViewInit(): void {
        this.sceneManager.initialize(this.canvasContainer);
    }

    ngOnDestroy(): void {
        this.clearWorkspace();
        this.sceneManager.dispose();
    }

    addMolecule(formula: string): void {
        if (this.elements.length === 0) {
            return;
        }

        const molecule = this.moleculeFactory.createMoleculeByFormula(formula, this.elements);

        if (molecule) {
            const offset = this.moleculeSpawnOffset * 4;
            molecule.group.position.set(offset - 6, 0, 0);
            this.moleculeSpawnOffset = (this.moleculeSpawnOffset + 1) % 4;

            this.molecules.push(molecule);
            this.sceneManager.addObject(molecule.id, molecule.group);
        }
    }

    clearWorkspace(): void {
        this.molecules.forEach(molecule => {
            this.sceneManager.removeObject(molecule.id);
            this.moleculeFactory.disposeMolecule(molecule);
        });
        this.molecules = [];
        this.moleculeSpawnOffset = 0;
    }

    resetCamera(): void {
        this.sceneManager.resetCamera();
    }
}