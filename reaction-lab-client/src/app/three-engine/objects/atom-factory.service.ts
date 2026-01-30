import * as THREE from 'three';
import { ElementSummary } from '../../core/models';
import { Injectable } from '@angular/core';

export interface Atom3D {
    id: string;
    group: THREE.Group;
    mesh: THREE.Mesh;
    element: ElementSummary;
    position: THREE.Vector3;
}

@Injectable({
    providedIn: 'root'
})
export class AtomFactoryService {
    private readonly defaultSegments = 32;

    createAtom(element: ElementSummary, position: THREE.Vector3 = new THREE.Vector3()): Atom3D {
        const group = new THREE.Group();
        group.position.copy(position);

        const radius = this.calculateRadius(element);
        const color = new THREE.Color(element.displayColor);

        const geometry = new THREE.SphereGeometry(radius, this.defaultSegments, this.defaultSegments);
        const material = new THREE.MeshPhongMaterial({
            color: color,
            shininess: 100,
            specular: new THREE.Color(0x444444),
            emissive: color.clone().multiplyScalar(0.1)
        });

        const mesh = new THREE.Mesh(geometry, material);
        mesh.castShadow = true;
        mesh.receiveShadow = true;
        group.add(mesh);

        const glowMesh = this.createGlowEffect(radius, color);
        group.add(glowMesh);

        const label = this.createLabel(element.symbol);
        label.position.set(0, radius + 0.3, 0);
        group.add(label);

        const id = crypto.randomUUID();
        group.userData = { id, elementId: element.id, type: 'atom' };

        return {
            id,
            group,
            mesh,
            element,
            position: group.position
        };
    }

    private calculateRadius(element: ElementSummary): number {
        const baseRadius = 0.5;
        const scaleFactor = 0.3;
        return baseRadius + (element.atomicNumber / 118) * scaleFactor;
    }

    private createGlowEffect(radius: number, color: THREE.Color): THREE.Mesh {
        const glowGeometry = new THREE.SphereGeometry(radius * 1.2, this.defaultSegments, this.defaultSegments);
        const glowMaterial = new THREE.MeshBasicMaterial({
            color: color,
            transparent: true,
            opacity: 0.15,
            side: THREE.BackSide
        });

        return new THREE.Mesh(glowGeometry, glowMaterial);
    }

    private createLabel(text: string): THREE.Sprite {
        const canvas = document.createElement('canvas');
        const context = canvas.getContext('2d')!;
        canvas.width = 128;
        canvas.height = 64;

        context.fillStyle = 'rgba(0, 0, 0, 0)';
        context.fillRect(0, 0, canvas.width, canvas.height);

        context.font = 'Bold 48px Arial';
        context.textAlign = 'center';
        context.textBaseline = 'middle';
        context.fillStyle = 'white';
        context.strokeStyle = 'black';
        context.lineWidth = 2;
        context.strokeText(text, canvas.width / 2, canvas.height / 2);
        context.fillText(text, canvas.width / 2, canvas.height / 2);

        const texture = new THREE.CanvasTexture(canvas);
        const material = new THREE.SpriteMaterial({
            map: texture,
            transparent: true
        });

        const sprite = new THREE.Sprite(material);
        sprite.scale.set(1, 0.5, 1);

        return sprite;
    }

    updateAtomPosition(atom: Atom3D, position: THREE.Vector3): void {
        atom.group.position.copy(position);
        atom.position.copy(position);
    }

    highlightAtom(atom: Atom3D, highlight: boolean): void {
        const material = atom.mesh.material as THREE.MeshPhongMaterial;
        if (highlight) {
            material.emissive = new THREE.Color(atom.element.displayColor).multiplyScalar(0.5);
        } else {
            material.emissive = new THREE.Color(atom.element.displayColor).multiplyScalar(0.1);
        }
    }

    disposeAtom(atom: Atom3D): void {
        atom.group.traverse((child) => {
            if (child instanceof THREE.Mesh) {
                child.geometry.dispose();

                if (Array.isArray(child.material)) {
                    child.material.forEach((mat) => mat.dispose());
                } else {
                    child.material.dispose();
                }
            }

            if (child instanceof THREE.Sprite) {
                child.material.map?.dispose();
                child.material.dispose();
            }
        });
    }
}