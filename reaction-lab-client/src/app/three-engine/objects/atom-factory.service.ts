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
    private readonly defaultSegments = 64;

    createAtom(element: ElementSummary, position: THREE.Vector3 = new THREE.Vector3()): Atom3D {
        const group = new THREE.Group();
        group.position.copy(position);

        const radius = this.calculateRadius(element);
        const color = new THREE.Color(element.displayColor);

        const texture = this.createAtomTexture(element.symbol, color);

        const geometry = new THREE.SphereGeometry(radius, this.defaultSegments, this.defaultSegments);
        const material = new THREE.MeshPhongMaterial({
            map: texture,
            shininess: 60,
            specular: new THREE.Color(0x333333)
        });

        const mesh = new THREE.Mesh(geometry, material);
        mesh.castShadow = true;
        mesh.receiveShadow = true;
        group.add(mesh);

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

    private createAtomTexture(symbol: string, atomColor: THREE.Color): THREE.CanvasTexture {
        const canvas = document.createElement('canvas');
        const context = canvas.getContext('2d')!;
        const size = 1024;
        canvas.width = size;
        canvas.height = size;

        context.fillStyle = `rgb(${Math.floor(atomColor.r * 255)}, ${Math.floor(atomColor.g * 255)}, ${Math.floor(atomColor.b * 255)})`;
        context.fillRect(0, 0, size, size);

        const gradient = context.createRadialGradient(
            size * 0.35, size * 0.35, 0,
            size * 0.5, size * 0.5, size * 0.5
        );
        gradient.addColorStop(0, 'rgba(255, 255, 255, 0.2)');
        gradient.addColorStop(0.5, 'rgba(255, 255, 255, 0)');
        gradient.addColorStop(1, 'rgba(0, 0, 0, 0.15)');
        context.fillStyle = gradient;
        context.fillRect(0, 0, size, size);

        const textColor = this.getContrastingColor(atomColor);
        const outlineColor = this.getOutlineColor(atomColor);

        const fontSize = symbol.length === 1 ? 100 : symbol.length === 2 ? 80 : 65;
        context.font = `Bold ${fontSize}px Arial`;
        context.textAlign = 'center';
        context.textBaseline = 'middle';

        context.strokeStyle = outlineColor;
        context.lineWidth = 8;
        context.strokeText(symbol, size / 2, size / 2);

        context.fillStyle = textColor;
        context.fillText(symbol, size / 2, size / 2);

        const texture = new THREE.CanvasTexture(canvas);
        texture.needsUpdate = true;

        return texture;
    }

    private getContrastingColor(color: THREE.Color): string {
        const luminance = 0.299 * color.r + 0.587 * color.g + 0.114 * color.b;

        return luminance > 0.5 ? '#1a1a2e' : '#ffffff';
    }

    private getOutlineColor(color: THREE.Color): string {
        const luminance = 0.299 * color.r + 0.587 * color.g + 0.114 * color.b;
        
        return luminance > 0.5 ? 'rgba(255, 255, 255, 0.3)' : 'rgba(0, 0, 0, 0.5)';
    }

    updateAtomPosition(atom: Atom3D, position: THREE.Vector3): void {
        atom.group.position.copy(position);
        atom.position.copy(position);
    }

    highlightAtom(atom: Atom3D, highlight: boolean): void {
        const material = atom.mesh.material as THREE.MeshPhongMaterial;
        if (highlight) {
            material.emissive = new THREE.Color(atom.element.displayColor).multiplyScalar(0.4);
            material.opacity = 1;
        } else {
            material.emissive = new THREE.Color(0x000000);
            material.opacity = 0.85;
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