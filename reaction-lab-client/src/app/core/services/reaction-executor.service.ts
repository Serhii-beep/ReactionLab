import * as THREE from 'three';
import { AnimationPhase, Atom3D, AtomFactoryService, Molecule3D, MoleculeFactoryService, ReactionAnimationService } from '../../three-engine';
import { ElementSummary, Reaction } from '../models';
import { inject, Injectable, signal } from '@angular/core';
import { gsap } from 'gsap';
import { MoleculeService } from './molecule.service';
import { firstValueFrom } from 'rxjs';

export interface SceneReactants {
    molecules: Map<string, Molecule3D[]>;
    atoms: Map<string, Atom3D[]>;
}

export interface ExecuteReactionInput {
    reaction: Reaction;
    sceneReactants: SceneReactants;
    elements: ElementSummary[];
    callbacks: {
        addMoleculeToScene: (molecule3D: Molecule3D) => void;
        addAtomToScene: (atom3D: Atom3D) => void;
        removeMoleculeFromScene: (id: string) => void;
        removeAtomFromScene: (id: string) => void;
    };
}

export interface ExecuteReactionResult {
    success: boolean;
    error?: string;
    producedMolecules: Molecule3D[];
    producedAtoms: Atom3D[];
    consumedMoleculeIds: string[];
    consumedAtomIds: string[];
}

export interface MissingReactant {
    type: 'molecule' | 'element';
    id: string;
    name: string;
    required: number;
    available: number;
    missing: number;
    template: Molecule3D | Atom3D;
}

@Injectable({
    providedIn: 'root'
})
export class ReactionExecutorService {
    private readonly animationService = inject(ReactionAnimationService);
    private readonly atomFactory = inject(AtomFactoryService);
    private readonly moleculeFactory = inject(MoleculeFactoryService);
    private readonly moleculeService = inject(MoleculeService);

    private readonly _isExecuting = signal(false);
    private readonly _currentPhase = signal<AnimationPhase>('idle');
    private readonly _error = signal<string | null>(null);

    readonly isExecuting = this._isExecuting.asReadonly();
    readonly currentPhase = this._currentPhase.asReadonly();
    readonly error = this._error.asReadonly();

    checkMissingReactants(reaction: Reaction, sceneReactants: SceneReactants): MissingReactant[] {
        const missing: MissingReactant[] = [];

        for (const reactant of reaction.reactants) {
            if (reactant.moleculeId) {
                const instances = sceneReactants.molecules.get(reactant.moleculeId) ?? [];
                const available = instances.length;
                const required = reactant.coefficient;

                if (available < required && available > 0) {
                    missing.push({
                        type: 'molecule',
                        id: reactant.moleculeId,
                        name: reactant.moleculeName ?? 'Unknown',
                        required,
                        available,
                        missing: required - available,
                        template: instances[0]
                    });
                }
            } else if (reactant.elementId) {
                const instances = sceneReactants.atoms.get(reactant.elementId) ?? [];
                const available = instances.length;
                const required = reactant.coefficient;

                if (available < required && available > 0) {
                    missing.push({
                        type: 'element',
                        id: reactant.elementId,
                        name: reactant.elementName ?? 'Unknown',
                        required,
                        available,
                        missing: required - available,
                        template: instances[0]
                    });
                }
            }
        }

        return missing;
    }

    async execute(input: ExecuteReactionInput): Promise<ExecuteReactionResult> {
        if (this._isExecuting()) {
            return this.errorResult('A reaction is already in progress');
        }

        this._isExecuting.set(true);
        this._error.set(null);
        this._currentPhase.set('idle');

        try {
            const { reaction, sceneReactants, elements, callbacks } = input;

            const missingReactants = this.checkMissingReactants(reaction, sceneReactants);
            const clonedItems = this.cloneMissingReactants(missingReactants, sceneReactants, callbacks);

            if (clonedItems.length > 0) {
                await this.delay(500);
            }

            const { moleculesToConsume, atomsToConsume } = this.collectReactantsToConsume(reaction, sceneReactants);

            const animationResult = await this.animationService.animate(
                {
                    reactantMolecules: moleculesToConsume,
                    reactantAtoms: atomsToConsume,
                    config: {
                        durationMs: reaction.animationDurationMs ?? 3000,
                        animationType: reaction.animationType ?? undefined,
                        effectPreset: reaction.effectPreset ?? undefined
                    }
                },
                (phase) => this._currentPhase.set(phase)
            );

            animationResult.consumedMoleculeIds.forEach(id => {
                callbacks.removeMoleculeFromScene(id);
            });
            animationResult.consumedAtomIds.forEach(id => {
                callbacks.removeAtomFromScene(id);
            });

            const { producedMolecules, producedAtoms } = await this.createProducts(
                reaction,
                animationResult.productSpawnCenter,
                sceneReactants,
                elements,
                callbacks
            );

            this._isExecuting.set(false);
            this._currentPhase.set('idle');

            return {
                success: true,
                producedMolecules,
                producedAtoms,
                consumedMoleculeIds: animationResult.consumedMoleculeIds,
                consumedAtomIds: animationResult.consumedAtomIds
            };
        } catch (error) {
            const errorMessage = error instanceof Error ? error.message : 'Unknown error';
            this._error.set(errorMessage);
            this._isExecuting.set(false);
            this._currentPhase.set('idle');

            return this.errorResult(errorMessage);
        }
    }

    stop(): void {
        this.animationService.stop();
        this._isExecuting.set(false);
        this._currentPhase.set('idle');
    }

    private cloneMissingReactants(
        missingReactants: MissingReactant[],
        sceneReactants: SceneReactants,
        callbacks: ExecuteReactionInput['callbacks']
    ): (Molecule3D | Atom3D)[] {
        const cloned: (Molecule3D | Atom3D)[] = [];
        const spawnCenter = this.calculateSpawnCenter(sceneReactants);

        for (const missing of missingReactants) {
            for (let i = 0; i < missing.missing; i++) {
                const position = this.calculateSpawnPosition(spawnCenter, i, missing.missing);

                if (missing.type === 'molecule') {
                    const template = missing.template as Molecule3D;
                    const clone = this.cloneMolecule3D(template, position);

                    callbacks.addMoleculeToScene(clone);
                    sceneReactants.molecules.get(missing.id)!.push(clone);
                    cloned.push(clone);

                    this.animateAppearing(clone.group);

                } else {
                    const template = missing.template as Atom3D;
                    const clone = this.cloneAtom3D(template, position);

                    callbacks.addAtomToScene(clone);
                    sceneReactants.atoms.get(missing.id)!.push(clone);
                    cloned.push(clone);

                    this.animateAppearing(clone.group);
                }
            }
        }

        return cloned;
    }

    private cloneMolecule3D(template: Molecule3D, position: THREE.Vector3): Molecule3D {
        const group = template.group.clone(true);
        const id = crypto.randomUUID();

        group.position.copy(position);
        group.userData = { ...template.group.userData, id };

        const atoms: Atom3D[] = [];
        const atomMeshes = group.children.filter(child => child.userData?.['type'] === 'atom') as THREE.Group[];

        template.atoms.forEach((templateAtom, index) => {
            if (atomMeshes[index]) {
                const atomGroup = atomMeshes[index];
                const mesh = atomGroup.children.find(c => c instanceof THREE.Mesh) as THREE.Mesh;

                atoms.push({
                    id: crypto.randomUUID(),
                    group: atomGroup,
                    mesh,
                    element: templateAtom.element,
                    position: atomGroup.position
                });
            }
        });

        const bonds = template.bonds.map(bond => ({
            ...bond,
            id: crypto.randomUUID(),
            group: group.children.find(child => child.userData?.['type'] === 'bond') as THREE.Group ?? bond.group.clone()
        }));

        return {
            id,
            group,
            atoms,
            bonds,
            molecule: template.molecule,
            boundingBox: new THREE.Box3().setFromObject(group)
        };
    }

    private cloneAtom3D(template: Atom3D, position: THREE.Vector3): Atom3D {
        const group = template.group.clone(true);
        const id = crypto.randomUUID();

        group.position.copy(position);
        group.userData = { ...template.group.userData, id };

        const mesh = group.children.find(c => c instanceof THREE.Mesh) as THREE.Mesh;

        return {
            id,
            group,
            mesh,
            element: template.element,
            position: group.position
        };
    }

    private collectReactantsToConsume(
        reaction: Reaction,
        sceneReactants: SceneReactants
    ): { moleculesToConsume: Molecule3D[]; atomsToConsume: Atom3D[] } {
        const moleculesToConsume: Molecule3D[] = [];
        const atomsToConsume: Atom3D[] = [];

        for (const reactant of reaction.reactants) {
            if (reactant.moleculeId) {
                const available = sceneReactants.molecules.get(reactant.moleculeId) ?? [];
                moleculesToConsume.push(...available.slice(0, reactant.coefficient));
            } else if (reactant.elementId) {
                const available = sceneReactants.atoms.get(reactant.elementId) ?? [];
                atomsToConsume.push(...available.slice(0, reactant.coefficient));
            }
        }

        return { moleculesToConsume, atomsToConsume };
    }

    private async createProducts(
        reaction: Reaction,
        spawnCenter: THREE.Vector3,
        sceneReactants: SceneReactants,
        elements: ElementSummary[],
        callbacks: ExecuteReactionInput['callbacks']
    ): Promise<{ producedMolecules: Molecule3D[]; producedAtoms: Atom3D[] }> {
        const producedMolecules: Molecule3D[] = [];
        const producedAtoms: Atom3D[] = [];

        const totalProducts = reaction.products.reduce((sum, p) => sum + p.coefficient, 0);
        let productIndex = 0;

        for (const product of reaction.products) {
            for (let i = 0; i < product.coefficient; i++) {
                const position = this.calculateProductPosition(spawnCenter, productIndex, totalProducts);

                if (product.moleculeId) {
                    const molecule3D = await this.createProductMolecule(
                        product.moleculeId,
                        position,
                        sceneReactants,
                        elements
                    );

                    if (molecule3D) {
                        callbacks.addMoleculeToScene(molecule3D);
                        producedMolecules.push(molecule3D);
                        this.animateAppearing(molecule3D.group);
                    }

                } else if (product.elementId) {
                    const atom3D = this.createProductAtom(
                        product.elementId,
                        position,
                        sceneReactants,
                        elements
                    );

                    if (atom3D) {
                        callbacks.addAtomToScene(atom3D);
                        producedAtoms.push(atom3D);
                        this.animateAppearing(atom3D.group);
                    }
                }

                productIndex++;
            }
        }

        return { producedMolecules, producedAtoms };
    }

    private async createProductMolecule(
        moleculeId: string,
        position: THREE.Vector3,
        sceneReactants: SceneReactants,
        elements: ElementSummary[]
    ): Promise<Molecule3D | null> {
        const existing = sceneReactants.molecules.get(moleculeId)?.[0];

        if (existing) {
            return this.cloneMolecule3D(existing, position);
        }

        try {
            const molecule = await firstValueFrom(this.moleculeService.getById(moleculeId));

            const molecule3D = this.moleculeFactory.createFromMolecule(molecule, elements);

            if (molecule3D) {
                molecule3D.group.position.copy(position);
                return molecule3D;
            }

            console.error(`Failed to create 3D model for product molecule: ${molecule.formula}`);
            return null;
        } catch (error) {
            console.error(`Failed to fetch product molecule ${moleculeId}:`, error);
            return null;
        }
    }

    private createProductAtom(
        elementId: string,
        position: THREE.Vector3,
        sceneReactants: SceneReactants,
        elements: ElementSummary[]
    ): Atom3D | null {
        const existing =sceneReactants.atoms.get(elementId)?.[0];

        if (existing) {
            return this.cloneAtom3D(existing, position);
        }

        const element = elements.find(e => e.id === elementId);
        if (element) {
            return this.atomFactory.createAtom(element, position);
        }

        console.error(`Element ${elementId} not found in the element list`);
        return null;
    }

    private calculateSpawnCenter(sceneReactants: SceneReactants): THREE.Vector3 {
        const positions: THREE.Vector3[] = [];

        sceneReactants.molecules.forEach(molecules => {
            molecules.forEach(mol => {
                const worldPos = new THREE.Vector3();
                mol.group.getWorldPosition(worldPos);
                positions.push(worldPos);
            });
        });

        sceneReactants.atoms.forEach(atoms => {
            atoms.forEach(atom => {
                const worldPos = new THREE.Vector3();
                atom.group.getWorldPosition(worldPos);
                positions.push(worldPos);
            });
        });

        if (positions.length === 0) {
            return new THREE.Vector3(0, 0, 0);
        }

        const center = new THREE.Vector3();
        positions.forEach(pos => center.add(pos));
        center.divideScalar(positions.length);

        return center;
    }

    private calculateSpawnPosition(center: THREE.Vector3, index: number, total: number): THREE.Vector3 {
        const angle = (index / total) * Math.PI * 2 + Math.random() * 0.3;
        const radius = 3 + Math.random() * 2;

        return new THREE.Vector3(
            center.x + Math.cos(angle) * radius,
            center.y,
            center.z + Math.sin(angle) * radius
        );
    }

    private calculateProductPosition(center: THREE.Vector3, index: number, total: number): THREE.Vector3 {
        const angle = (index / total) * Math.PI * 2;
        const radius = 2 + (index % 2) * 1.5;

        return new THREE.Vector3(
            center.x + Math.cos(angle) * radius,
            center.y,
            center.z + Math.sin(angle) * radius
        );
    }

    private animateAppearing(group: THREE.Group): void {
        group.scale.setScalar(0);

        gsap.to(group.scale, {
            x: 1,
            y: 1,
            z: 1,
            duration: 0.5,
            ease: 'elastic.out(1, 0.5)'
        });

        group.traverse(child => {
            if (child instanceof THREE.Mesh) {
                const material = child.material as THREE.MeshPhongMaterial;
                if (material.emissive) {
                    const originalEmissive = material.emissive.clone();
                    material.emissive.setHex(0x4fc3f7);
                    material.emissiveIntensity = 1;

                    gsap.to(material, {
                        emissiveIntensity: 0,
                        duration: 0.8,
                        ease: 'power2.out',
                        onComplete: () => {
                            material.emissive.copy(originalEmissive);
                        }
                    });
                }
            }
        });
    }

    private errorResult(error: string): ExecuteReactionResult {
        return {
            success: false,
            error,
            producedMolecules: [],
            producedAtoms: [],
            consumedMoleculeIds: [],
            consumedAtomIds: []
        };
    }

    private delay(ms: number): Promise<void> {
        return new Promise(resolve => setTimeout(resolve, ms));
    }
}