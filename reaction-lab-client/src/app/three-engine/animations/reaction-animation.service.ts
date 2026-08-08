import * as THREE from 'three';
import { Atom3D, AtomFactoryService, Bond3D, BondFactoryService, Molecule3D, MoleculeFactoryService } from '../objects';
import { inject, Injectable } from '@angular/core';
import { gsap } from 'gsap';
import { ElementSummary, MoleculeStructure, Reaction, ReactionParticipant } from '../../core/models';

export interface ReactionAnimationConfig {
    durationMs: number;
    animationType?: string;
    effectPreset?: string;
}

export interface ReactionAnimationInput {
    reactantMolecules: Molecule3D[];
    reactantAtoms: Atom3D[];
    config: ReactionAnimationConfig;
}

export interface ReactionAnimationResult {
    productSpawnCenter: THREE.Vector3;
    consumedMoleculeIds: string[];
    consumedAtomIds: string[];
}

export type AnimationPhase =
    | 'idle'
    | 'gathering'
    | 'breaking'
    | 'transforming'
    | 'complete';

@Injectable({
    providedIn: 'root'
})
export class ReactionAnimationService {
    private timeline: gsap.core.Timeline | null = null;
    private currentPhase: AnimationPhase = 'idle';
    private onPhaseChange?: (phase: AnimationPhase) => void;

    async animate(input: ReactionAnimationInput, onPhaseChange?: (phase: AnimationPhase) => void): Promise<ReactionAnimationResult> {
        this.onPhaseChange = onPhaseChange;

        this.stop();

        const { reactantMolecules, reactantAtoms, config } = input;

        const totalDuration = config.durationMs / 1000;
        const phaseDuration = totalDuration / 3;

        const center = this.calculateCenter(reactantMolecules, reactantAtoms);
        const allBonds = this.collectBonds(reactantMolecules);
        const allAtoms = this.collectAllAtoms(reactantMolecules, reactantAtoms);

        return new Promise((resolve) => {
            this.timeline = gsap.timeline({
                onComplete: () => {
                    this.setPhase('complete');

                    resolve({
                        productSpawnCenter: center,
                        consumedMoleculeIds: reactantMolecules.map(m => m.id),
                        consumedAtomIds: reactantAtoms.map(a => a.id)
                    });
                }
            });

            this.addGatherPhase(reactantMolecules, reactantAtoms, center, phaseDuration);

            this.addBreakPhase(allBonds, phaseDuration);

            this.addTransformPhase(allAtoms, allBonds, center, phaseDuration, config);

            this.timeline.play();
        })
    }

    stop(): void {
        if (this.timeline) {
            this.timeline.kill();
            this.timeline = null;
        }

        this.setPhase('idle');
    }

    pause(): void {
        this.timeline?.pause();
    }

    resume(): void {
        this.timeline?.resume();
    }

    getProgress(): number {
        return this.timeline?.progress() ?? 0;
    }

    getPhase(): AnimationPhase {
        return this.currentPhase;
    }

    isAnimating(): boolean {
        return this.timeline !== null && this.timeline.isActive();
    }

    private setPhase(phase: AnimationPhase): void {
        if (this.currentPhase !== phase) {
            this.currentPhase = phase;
            this.onPhaseChange?.(phase);
        }
    }

    private calculateCenter(molecules: Molecule3D[], atoms: Atom3D[]): THREE.Vector3 {
        const positions: THREE.Vector3[] = [];

        molecules.forEach(mol => {
            const worldPos = new THREE.Vector3();
            mol.group.getWorldPosition(worldPos);
            positions.push(worldPos);
        });

        atoms.forEach(atom => {
            const worldPos = new THREE.Vector3();
            atom.group.getWorldPosition(worldPos);
            positions.push(worldPos);
        });

        if (positions.length === 0) {
            return new THREE.Vector3(0, 0, 0);
        }

        const center = new THREE.Vector3();
        positions.forEach(pos => center.add(pos));
        center.divideScalar(positions.length);

        return center;
    }

    private collectAllAtoms(molecules: Molecule3D[], standaloneAtoms: Atom3D[]): Atom3D[] {
        const atoms: Atom3D[] = [];

        molecules.forEach(mol => {
            atoms.push(...mol.atoms);
        });

        atoms.push(...standaloneAtoms);

        return atoms;
    }

    private collectBonds(molecules: Molecule3D[]): Bond3D[] {
        const bonds: Bond3D[] = [];
        molecules.forEach(mol => {
            bonds.push(...mol.bonds);
        });
        return bonds;
    }

    private addGatherPhase(
        molecules: Molecule3D[],
        atoms: Atom3D[],
        center: THREE.Vector3,
        duration: number
    ): void {
        if (!this.timeline) {
            return;
        }

        this.timeline.call(() => this.setPhase('gathering'));

        const gatherDistance = 1.5;

        molecules.forEach((mol, index) => {
            const worldPos = new THREE.Vector3();
            mol.group.getWorldPosition(worldPos);

            const direction = new THREE.Vector3()
                .subVectors(center, worldPos)
                .normalize();

            const distance = worldPos.distanceTo(center);
            const moveDistance = Math.max(0, distance - gatherDistance);
            
            const targetPos = worldPos.clone()
                .add(direction.multiplyScalar(moveDistance));
            
            this.timeline!.to(
                mol.group.position,
                {
                    x: targetPos.x,
                    y: targetPos.y,
                    z: targetPos.z,
                    duration,
                    ease: 'power2.inOut'
                },
                index === 0 ? '>' : '<'
            );

            this.timeline!.to(
                mol.group.rotation,
                {
                    y: mol.group.rotation.y + Math.PI * 0.5,
                    duration,
                    ease: 'power2.inOut'
                },
                '<'
            );
        });

        atoms.forEach((atom) => {
            const worldPos = new THREE.Vector3();
            atom.group.getWorldPosition(worldPos);

            const direction = new THREE.Vector3()
                .subVectors(center, worldPos)
                .normalize();
            
            const distance = worldPos.distanceTo(center);
            const moveDistance = Math.max(0, distance - gatherDistance);
            
            const targetPos = worldPos.clone()
                .add(direction.multiplyScalar(moveDistance));
            
            this.timeline!.to(
                atom.group.position,
                {
                    x: targetPos.x,
                    y: targetPos.y,
                    z: targetPos.z,
                    duration,
                    ease: 'power2.inOut'
                },
                '<'
            );
        });

        if (molecules.length === 0 && atoms.length === 0) {
            this.timeline.to({}, { duration });
        }
    }

    private addBreakPhase(bonds: Bond3D[], duration: number): void {
        if (!this.timeline) {
            return;
        }

        this.timeline.call(() => this.setPhase('breaking'));

        bonds.forEach(bond => {
            bond.group.traverse(child => {
                if (child instanceof THREE.Mesh) {
                    const material = child.material as THREE.MeshPhongMaterial;
                    material.transparent = true;

                    this.timeline!.to(
                        child.scale,
                        {
                            x: 1.3,
                            y: 1.1,
                            z: 1.3,
                            duration: duration * 0.4,
                            ease: 'power2.out'
                        },
                        '<'
                    );

                    this.animateMaterialEmissive(material, 0.8, duration * 0.3, '<');

                    this.timeline!.to(
                        material,
                        {
                            opacity: 0,
                            duration: duration * 0.6,
                            ease: 'power2.in'
                        },
                        '<+=' + (duration * 0.4)
                    );

                    this.animateMaterialEmissive(material, 0, duration * 0.3, '<');
                }
            });
        });

        if (bonds.length === 0) {
            this.timeline.to({}, { duration });
        }
    }

    private addTransformPhase(
        atoms: Atom3D[],
        bonds: Bond3D[],
        center: THREE.Vector3,
        duration: number,
        config: ReactionAnimationConfig
    ): void {
        if (!this.timeline) {
            return;
        }

        this.timeline.call(() => this.setPhase('transforming'));

        const isExothermic = config.effectPreset === 'fire' || config.animationType === 'combustion';
        const glowColor = isExothermic ? new THREE.Color(0xff6600) : new THREE.Color(0x4fc3f7);

        atoms.forEach((atom, index) => {
            const material = atom.mesh.material as THREE.MeshPhongMaterial;
            const angle = (index / atoms.length) * Math.PI * 2;
            const swirlRadius = 0.5;

            material.emissive = glowColor;

            const swirlTarget = {
                x: center.x + Math.cos(angle) * swirlRadius,
                y: center.y + Math.sin(index * 0.5) * 0.3,
                z: center.z + Math.sin(angle) * swirlRadius
            };

            this.timeline!.to(
                atom.group.position,
                {
                    x: swirlTarget.x,
                    y: swirlTarget.y,
                    z: swirlTarget.z,
                    duration: duration * 0.5,
                    ease: 'power2.inOut'
                },
                '<'
            );

            this.animateMaterialEmissive(material, 1.5, duration * 0.3, '<');

            this.timeline!.to(
                atom.group.position,
                {
                    x: center.x,
                    y: center.y,
                    z: center.z,
                    duration: duration * 0.3,
                    ease: 'power2.in'
                },
                '<+=' + (duration * 0.5)
            );

            this.timeline!.to(
                atom.group.scale,
                {
                    x: 0,
                    y: 0,
                    z: 0,
                    duration: duration * 0.3,
                    ease: 'power2.in'
                },
                '<'
            );

            material.transparent = true;
            this.timeline!.to(
                material,
                {
                    opacity: 0,
                    duration: duration * 0.2,
                    ease: 'power2.in'
                },
                '<+=' + (duration * 0.1)
            );
        });

        if (atoms.length === 0) {
            this.timeline.to({}, { duration });
        }
    }

    private animateMaterialEmissive(
        material: THREE.MeshPhongMaterial,
        targetIntensity: number,
        duration: number,
        position: string
    ): void {
        if (!this.timeline) {
            return;
        }

        this.timeline.to(
            material,
            {
                emissiveIntensity: targetIntensity,
                duration,
                ease: 'power2.inOut'
            },
            position
        );
    }
}