export interface Composition {
    readonly symbol: string;
    readonly count: number;
}

const TOKEN = /([A-Z][a-z]?)(\d*)/g;

export function parseHillFormula(formula: string): readonly Composition[] {
    return [...formula.matchAll(TOKEN)].map(([, symbol, count]) => ({ symbol, count: count === '' ? 1 : Number(count) }));
}