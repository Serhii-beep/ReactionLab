export type ElementCategory =
    | 'AlkaliMetal'
    | 'AlkalineEarthMetal'
    | 'TransitionMetal'
    | 'PostTransitionMetal'
    | 'Metalloid'
    | 'NonMetal'
    | 'Halogen'
    | 'NobleGas'
    | 'Lanthanide'
    | 'Actinide'
    | 'Unknown';

export interface ElementSummary {
    readonly id: string;
    readonly atomicNumber: number;
    readonly symbol: string;
    readonly name: string;
    readonly mass: number;
    readonly category: ElementCategory;
    readonly period: number;
    readonly group: number | null;
}