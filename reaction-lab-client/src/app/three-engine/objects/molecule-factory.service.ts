import * as THREE from 'three';
import { Atom3D, AtomFactoryService } from './atom-factory.service';
import { Bond3D, BondFactoryService } from './bond-factory.service';
import { ElementSummary, Molecule } from '../../core/models';
import { BondType } from '../../core/models/bond.model';
import { inject, Injectable } from '@angular/core';

export interface Molecule3D {
    id: string;
    group: THREE.Group;
    atoms: Atom3D[];
    bonds: Bond3D[];
    molecule: Molecule | null;
    boundingBox: THREE.Box3;
}

export interface SimpleMoleculeConfig {
    elements: ElementSummary[];
    positions: THREE.Vector3[];
    bonds: { from: number; to: number; type: BondType }[];
}

@Injectable({
    providedIn: 'root'
})
export class MoleculeFactoryService {
    private readonly atomFactory = inject(AtomFactoryService);
    private readonly bondFactory = inject(BondFactoryService);

    createMoleculeFromConfig(config: SimpleMoleculeConfig): Molecule3D {
        const group = new THREE.Group();
        const atoms: Atom3D[] = [];
        const bonds: Bond3D[] = [];
        const id = crypto.randomUUID();

        config.elements.forEach((element, index) => {
            const position = config.positions[index] || new THREE.Vector3();
            const atom = this.atomFactory.createAtom(element, position);
            atoms.push(atom);
            group.add(atom.group);
        });

        config.bonds.forEach((bondConfig) => {
            const startAtom = atoms[bondConfig.from];
            const endAtom = atoms[bondConfig.to];

            if (startAtom && endAtom) {
                const bond = this.bondFactory.createBond(startAtom.position, endAtom.position, bondConfig.type);
                bonds.push(bond);
                group.add(bond.group);
            }
        });

        const boundingBox = new THREE.Box3().setFromObject(group);

        const center = boundingBox.getCenter(new THREE.Vector3());
        group.position.sub(center);

        group.userData = { id, type: 'molecule' };

        return {
            id,
            group,
            atoms,
            bonds,
            molecule: null,
            boundingBox
        };
    }

    createWaterMolecule(elements: ElementSummary[]): Molecule3D {
        const hydrogen = elements.find(e => e.symbol === 'H');
        const oxygen = elements.find(e => e.symbol === 'O');

        if (!hydrogen || !oxygen) {
            throw new Error('Hydrogen and Oxygen elements are required');
        }

        const angle = (104.5 * Math.PI) / 180;
        const bondLength = 1.5;

        const config: SimpleMoleculeConfig = {
            elements: [oxygen, hydrogen, hydrogen],
            positions: [
                new THREE.Vector3(0, 0, 0),
                new THREE.Vector3(bondLength * Math.sin(angle / 2), bondLength * Math.cos(angle / 2), 0),
                new THREE.Vector3(-bondLength * Math.sign(angle / 2), bondLength * Math.cos(angle / 2), 0)
            ],
            bonds: [
                { from: 0, to: 1, type: BondType.Covalent },
                { from: 0, to: 2, type: BondType.Covalent }
            ]
        };

        return this.createMoleculeFromConfig(config);
    }

    createCO2Molecule(elements: ElementSummary[]): Molecule3D {
        const carbon = elements.find(e => e.symbol === 'C');
        const oxygen = elements.find(e => e.symbol === 'O');

        if (!carbon || !oxygen) {
            throw new Error('Carbon and Oxygen elements are required');
        }

        const bondLength = 1.6;

        const config: SimpleMoleculeConfig = {
            elements: [oxygen, carbon, oxygen],
            positions: [
                new THREE.Vector3(-bondLength, 0, 0),
                new THREE.Vector3(0, 0, 0),
                new THREE.Vector3(bondLength, 0, 0)
            ],
            bonds: [
                { from: 0, to: 1, type: BondType.Double },
                { from: 1, to: 2, type: BondType.Double }
            ]
        };

        return this.createMoleculeFromConfig(config);
    }

    createMethaneMolecule(elements: ElementSummary[]): Molecule3D {
        const carbon = elements.find(e => e.symbol === 'C');
        const hydrogen = elements.find(e => e.symbol === 'H');

        if (!carbon || !hydrogen) {
            throw new Error('Carbon and Hydrogen elements are required');
        }

        const bondLength = 1.4;
        const tetrahedralAngle = Math.acos(-1/3);

        const config: SimpleMoleculeConfig = {
            elements: [carbon, hydrogen, hydrogen, hydrogen, hydrogen],
            positions: [
                new THREE.Vector3(0, 0, 0),
                new THREE.Vector3(0, bondLength, 0),
                new THREE.Vector3(bondLength * Math.sign(tetrahedralAngle), -bondLength * Math.cos(tetrahedralAngle), 0),
                new THREE.Vector3(
                    bondLength * Math.sin(tetrahedralAngle) * Math.cos(2 * Math.PI / 3),
                    -bondLength * Math.cos(tetrahedralAngle),
                    bondLength * Math.sin(tetrahedralAngle) * Math.sin(2 * Math.PI / 3)
                ),
                new THREE.Vector3(
                    bondLength * Math.sin(tetrahedralAngle) * Math.cos(4 * Math.PI / 3),
                    -bondLength * Math.cos(tetrahedralAngle),
                    bondLength * Math.sin(tetrahedralAngle) * Math.sin(4 * Math.PI / 3)
                )
            ],
            bonds: [
                { from: 0, to: 1, type: BondType.Covalent },
                { from: 0, to: 2, type: BondType.Covalent },
                { from: 0, to: 3, type: BondType.Covalent },
                { from: 0, to: 4, type: BondType.Covalent }
            ]
        };

        return this.createMoleculeFromConfig(config);
    }

    createO2Molecule(elements: ElementSummary[]): Molecule3D {
        const oxygen = elements.find(e => e.symbol === 'O');

        if (!oxygen) {
        throw new Error('Oxygen element is required');
        }

        const bondLength = 1.2;

        const config: SimpleMoleculeConfig = {
        elements: [oxygen, oxygen],
        positions: [
            new THREE.Vector3(-bondLength / 2, 0, 0),
            new THREE.Vector3(bondLength / 2, 0, 0)
        ],
        bonds: [
            { from: 0, to: 1, type: BondType.Double }
        ]
        };

        return this.createMoleculeFromConfig(config);
    }

  createH2Molecule(elements: ElementSummary[]): Molecule3D {
        const hydrogen = elements.find(e => e.symbol === 'H');

        if (!hydrogen) {
        throw new Error('Hydrogen element is required');
        }

        const bondLength = 0.74;

        const config: SimpleMoleculeConfig = {
        elements: [hydrogen, hydrogen],
        positions: [
            new THREE.Vector3(-bondLength / 2, 0, 0),
            new THREE.Vector3(bondLength / 2, 0, 0)
        ],
        bonds: [
            { from: 0, to: 1, type: BondType.Covalent }
        ]
        };

        return this.createMoleculeFromConfig(config);
    }

    createMoleculeByFormula(formula: string, elements: ElementSummary[]): Molecule3D | null {
        switch (formula.toUpperCase()) {
            case 'H2O':
                return this.createWaterMolecule(elements);
            case 'CO2':
                return this.createCO2Molecule(elements);
            case 'CH4':
                return this.createMethaneMolecule(elements);
            case 'O2':
                return this.createO2Molecule(elements);
            case 'H2':
                return this.createH2Molecule(elements);
            default:
                return null;
        }
    }

    setMoleculePosition(molecule: Molecule3D, position: THREE.Vector3): void {
        molecule.group.position.copy(position);
    }

    rotateMolecule(molecule: Molecule3D, axis: THREE.Vector3, angle: number): void {
        molecule.group.rotateOnAxis(axis, angle);
    }

    scaleMolecule(molecule: Molecule3D, scale: number): void {
        molecule.group.scale.setScalar(scale);
    }

    highlightMolecule(molecule: Molecule3D, highlight: boolean): void {
        molecule.atoms.forEach(atom => {
            this.atomFactory.highlightAtom(atom, highlight);
        });
    }

    disposeMolecule(molecule: Molecule3D): void {
        molecule.atoms.forEach(atom => this.atomFactory.disposeAtom(atom));
        molecule.bonds.forEach(bond => this.bondFactory.disposeBond(bond));
    }
}