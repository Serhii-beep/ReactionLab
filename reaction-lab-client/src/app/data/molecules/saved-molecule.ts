import { isMoleculeSketch, MoleculeSketch } from "./molecule-sketch";

export interface SavedMolecule {
    readonly id: string;
    readonly name: string;
    readonly hillFormula: string;
    readonly gramsPerMole: number | null;
    readonly sketch: MoleculeSketch;
    readonly positionsAngstroms: readonly number[];
    readonly savedAt: string;
}

export function isSavedMolecule(value: unknown): value is SavedMolecule {
    return typeof value === 'object' && value !== null
        && hasDescription(value as Record<string, unknown>)
        && hasGeometry(value as Record<string, unknown>);
}

function hasDescription(candidate: Record<string, unknown>): boolean {
    return typeof candidate['id'] === 'string'
        && typeof candidate['name'] === 'string'
        && typeof candidate['hillFormula'] === 'string'
        && typeof candidate['savedAt'] === 'string'
        && (candidate['gramsPerMole'] === null || typeof candidate['gramsPerMole'] === 'number');
}

function hasGeometry(candidate: Record<string, unknown>): boolean {
    const sketch = candidate['sketch'];
    const positions = candidate['positionsAngstroms'];

    return isMoleculeSketch(sketch)
        && Array.isArray(positions)
        && positions.length === 3 * sketch.atoms.length
        && positions.every((coordinate) => typeof coordinate === 'number');
}
