/**
 * Schema for 3D molecular structure stored in database
 */

export interface MoleculeStructure {
    atoms: AtomStructure[];
    bonds: BondStructure[];
}

export interface AtomStructure {
    symbol: string;
    position: [number, number, number];
    label?: string;
}

export interface BondStructure {
    from: number;
    to: number;
    type: BondStructureType;
}

export type BondStructureType =
    | 'signle'
    | 'double'
    | 'triple'
    | 'ionic'
    | 'covalent'
    | 'hydrogen'
    | 'metallic';

export function parseMoleculeStructure(json: string | null): MoleculeStructure | null {
    if (!json) {
        return null;
    }

    try {
        const parsed = JSON.parse(json);

        if (!Array.isArray(parsed.atoms) || !Array.isArray(parsed.bonds)) {
            console.warn('Invalid molecule structure: missing atoms or bonds array');
            return null;
        }

        return parsed as MoleculeStructure;
    } catch (error) {
        console.error('Failed to parse molecule structure:', error);
        return null;
    }
}

export function toBondType(type: BondStructureType): number {
    const mapping: Record<BondStructureType, number> = {
        'signle': 0,
        'double': 1,
        'triple': 2,
        'ionic': 3,
        'hydrogen': 4,
        'metallic': 5,
        'covalent': 6,
    };

    return mapping[type] ?? 0;
}