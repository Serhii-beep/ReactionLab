import * as THREE from 'three';
import { Atom3D, AtomFactoryService } from './atom-factory.service';
import { Bond3D, BondFactoryService } from './bond-factory.service';
import { BondStructureType, BondType, ElementSummary, Molecule, MoleculeStructure, parseMoleculeStructure } from '../../core/models';
import { inject, Injectable } from '@angular/core';

export interface Molecule3D {
    id: string;
    group: THREE.Group;
    atoms: Atom3D[];
    bonds: Bond3D[];
    molecule: Molecule | null;
    boundingBox: THREE.Box3;
}

@Injectable({
    providedIn: 'root'
})
export class MoleculeFactoryService {
    private readonly atomFactory = inject(AtomFactoryService);
    private readonly bondFactory = inject(BondFactoryService);

    createFromMolecule(molecule: Molecule, elements: ElementSummary[]): Molecule3D | null {
        const structure = parseMoleculeStructure(molecule.structure3D);

        if (!structure) {
            console.warn(`No valid structure data for molecule: ${molecule.formula}`);
            return null;
        }

        return this.createFromStructure(structure, elements, molecule);
    }

    createFromJson(json: string, elements: ElementSummary[], molecule: Molecule | null = null): Molecule3D | null {
        const structure = parseMoleculeStructure(json);

        if (!structure) {
            return null;
        }

        return this.createFromStructure(structure, elements, molecule);
    }

    createFromStructure(structure: MoleculeStructure, elements: ElementSummary[], molecule: Molecule | null = null): Molecule3D | null {
        const group = new THREE.Group();
        const atoms: Atom3D[] = [];
        const bonds: Bond3D[] = [];
        const id = crypto.randomUUID();

        const elementMap = new Map<string, ElementSummary>();
        elements.forEach(el => elementMap.set(el.symbol, el));

        for (const atomData of structure.atoms) {
            const element = elementMap.get(atomData.symbol);

            if (!element) {
                console.warn(`Element not found: ${atomData.symbol}`);
                continue;
            }

            const position = new THREE.Vector3(
                atomData.position[0],
                atomData.position[1],
                atomData.position[2]
            );

            const atom = this.atomFactory.createAtom(element, position);
            atoms.push(atom);
            group.add(atom.group);
        }

        for (const bondData of structure.bonds) {
            const startAtom = atoms[bondData.from];
            const endAtom = atoms[bondData.to];

            if (!startAtom || !endAtom) {
                console.warn(`Invalid bond indices: ${bondData.from} -> ${bondData.to}`);
                continue;
            }

            const bondType = this.mapBondType(bondData.type);
            const bond = this.bondFactory.createBond(
                startAtom.position,
                endAtom.position,
                bondType
            );
            bonds.push(bond);
            group.add(bond.group);
        }

        const boundingBox = new THREE.Box3().setFromObject(group);

        const center = boundingBox.getCenter(new THREE.Vector3());
        group.position.sub(center);

        group.userData = {
            id,
            type: 'molecule',
            formula: molecule?.formula || 'unknown'
        };

        return {
            id,
            group,
            atoms,
            bonds,
            molecule,
            boundingBox
        };
    }

    private mapBondType(type: BondStructureType): BondType {
        const mapping: Record<BondStructureType, BondType> = {
            'signle': BondType.Single,
            'double': BondType.Double,
            'triple': BondType.Triple,
            'ionic': BondType.Ionic,
            'hydrogen': BondType.Hydrogen,
            'metallic': BondType.Metallic,
            'covalent': BondType.Covalent
        };

        return mapping[type] ?? BondType.Single;
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