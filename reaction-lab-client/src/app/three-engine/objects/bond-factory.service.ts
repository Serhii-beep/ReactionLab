import { Injectable } from '@angular/core';
import * as THREE from 'three';
import { BondType } from '../../core/models/bond.model';

export interface Bond3D {
    id: string;
    group: THREE.Group;
    bondType: BondType;
    startPosition: THREE.Vector3,
    endPosition: THREE.Vector3
}

@Injectable({
    providedIn: 'root'
})
export class BondFactoryService {
    private readonly bondRadius = 0.05;
    private readonly bondSpacing = 0.15;
    private readonly bondColors = {
        single: 0xcccccc,
        double: 0xcccccc,
        triple: 0xcccccc,
        ionic: 0x88ccff,
        hydrogen: 0x88ff88,
        metallic: 0xffcc44,
        covalent: 0xcccccc
    };

    createBond(startPosition: THREE.Vector3, endPosition: THREE.Vector3, bondType: BondType = BondType.Single): Bond3D {
        const group = new THREE.Group();
        const id = crypto.randomUUID();

        switch (bondType) {
            case BondType.Single:
            case BondType.Covalent:
                this.createSingleBond(group, startPosition, endPosition);
                break;
            case BondType.Double:
                this.createDoubleBond(group, startPosition, endPosition);
                break;
            case BondType.Triple:
                this.createTripleBond(group, startPosition, endPosition);
                break;
            case BondType.Ionic:
                this.createIonicBond(group, startPosition, endPosition);
                break;
            case BondType.Hydrogen:
                this.createHydrogenBond(group, startPosition, endPosition);
                break;
            case BondType.Metallic:
                this.createMetallicBond(group, startPosition, endPosition);
                break;
            default:
                this.createSingleBond(group, startPosition, endPosition);
        }

        group.userData = { id, type: 'bond', bondType };

        return {
            id,
            group,
            bondType,
            startPosition: startPosition.clone(),
            endPosition: endPosition.clone()
        };
    }

    private createSingleBond(group: THREE.Group, start: THREE.Vector3, end: THREE.Vector3): void {
        const cylinder = this.createCylinder(start, end, this.bondRadius, this.bondColors.single);
        group.add(cylinder);
    }

    private createDoubleBond(group: THREE.Group, start: THREE.Vector3, end: THREE.Vector3): void {
        const direction = new THREE.Vector3().subVectors(end, start).normalize();
        const perpendicular = this.getPerpendicular(direction).multiplyScalar(this.bondSpacing);

        const start1 = start.clone().add(perpendicular);
        const end1 = end.clone().add(perpendicular);
        const start2 = start.clone().sub(perpendicular);
        const end2 = end.clone().sub(perpendicular);

        group.add(this.createCylinder(start1, end1, this.bondRadius, this.bondColors.double));
        group.add(this.createCylinder(start2, end2, this.bondRadius, this.bondColors.double));
    }

    private createTripleBond(group: THREE.Group, start: THREE.Vector3, end: THREE.Vector3): void {
        const direction = new THREE.Vector3().subVectors(end, start).normalize();
        const perpendicular = this.getPerpendicular(direction).multiplyScalar(this.bondSpacing);

        group.add(this.createCylinder(start, end, this.bondRadius, this.bondColors.triple));

        const start1 = start.clone().add(perpendicular);
        const end1 = end.clone().add(perpendicular);
        const start2 = start.clone().sub(perpendicular);
        const end2 = end.clone().sub(perpendicular);

        group.add(this.createCylinder(start1, end1, this.bondRadius, this.bondColors.triple));
        group.add(this.createCylinder(start2, end2, this.bondRadius, this.bondColors.triple));
    }

    private createIonicBond(group: THREE.Group, start: THREE.Vector3, end: THREE.Vector3): void {
        const segments = 8;
        const direction = new THREE.Vector3().subVectors(end, start);
        const segmentLength = direction.length() / (segments * 2);
        direction.normalize();

        for (let i = 0; i < segments; i++) {
            const segmentStart = start.clone().add(direction.clone().multiplyScalar(i * segmentLength * 2));
            const segmentEnd = segmentStart.clone().add(direction.clone().multiplyScalar(segmentLength));
            group.add(this.createCylinder(segmentStart, segmentEnd, this.bondRadius * 0.8, this.bondColors.ionic));
        }
    }

    private createHydrogenBond(group: THREE.Group, start: THREE.Vector3, end: THREE.Vector3): void {
        const segments = 12;
        const direction = new THREE.Vector3().subVectors(end, start);
        const totalLength = direction.length();
        direction.normalize();

        for (let i = 0; i < segments; i++) {
            const t = i / segments;
            const position = start.clone().add(direction.clone().multiplyScalar(t * totalLength));

            const dotGeometry = new THREE.SphereGeometry(this.bondRadius * 0.6, 8, 8);
            const dotMaterial = new THREE.MeshPhongMaterial({
                color: this.bondColors.hydrogen
            });
            const dot = new THREE.Mesh(dotGeometry, dotMaterial);
            dot.position.copy(position);
            group.add(dot);
        }
    }

    private createMetallicBond(group: THREE.Group, start: THREE.Vector3, end: THREE.Vector3): void {
        const cylinder = this.createCylinder(start, end, this.bondRadius * 1.5, this.bondColors.metallic);
        const material = cylinder.material as THREE.MeshPhongMaterial;
        material.shininess = 150;
        material.specular = new THREE.Color(0xffffff);
        group.add(cylinder);
    }

    private createCylinder(start: THREE.Vector3, end: THREE.Vector3, radius: number, color: number): THREE.Mesh {
        const direction = new THREE.Vector3().subVectors(end, start);
        const length = direction.length();

        const geometry = new THREE.CylinderGeometry(radius, radius, length, 16);
        const material = new THREE.MeshPhongMaterial({
            color,
            shininess: 30
        });

        const cylinder = new THREE.Mesh(geometry, material);
        cylinder.castShadow = true;

        const midpoint = new THREE.Vector3().addVectors(start, end).multiplyScalar(0.5);
        cylinder.position.copy(midpoint);

        const axis = new THREE.Vector3(0, 1, 0);
        const quaternion = new THREE.Quaternion().setFromUnitVectors(axis, direction.clone().normalize());
        cylinder.quaternion.copy(quaternion);

        return cylinder;
    }

    private getPerpendicular(direction: THREE.Vector3): THREE.Vector3 {
        const arbitrary = Math.abs(direction.x) < 0.9
            ? new THREE.Vector3(1, 0, 0)
            : new THREE.Vector3(0, 1, 0);
        
        return new THREE.Vector3().crossVectors(direction, arbitrary).normalize();
    }

    updateBondPositions(bond: Bond3D, startPosition: THREE.Vector3, endPosition: THREE.Vector3): void {
        bond.group.clear();

        const newBond = this.createBond(startPosition, endPosition, bond.bondType);
        bond.group.add(...newBond.group.children);
        bond.startPosition.copy(startPosition);
        bond.endPosition.copy(endPosition);
    }

    disposeBond(bond: Bond3D): void {
        bond.group.traverse((child) => {
            if (child instanceof THREE.Mesh) {
                child.geometry.dispose();
                if (Array.isArray(child.material)) {
                    child.material.forEach((mat) => mat.dispose());
                } else {
                    child.material.dispose();
                }
            }
        });
    }
}