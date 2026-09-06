export interface GridPosition {
    readonly row: number;
    readonly column: number;
}

export type ArrowKey = 'ArrowUp' | 'ArrowDown' | 'ArrowLeft' | 'ArrowRight' | 'Home' | 'End';

export function isArrowKey(key: string): key is ArrowKey {
    return key === 'ArrowUp' || key === 'ArrowDown' || key === 'ArrowLeft' || key === 'ArrowRight' || key === 'Home' || key === 'End';
}

export function neighbor<T extends GridPosition>(cells: readonly T[], from: T, key: ArrowKey): T | null {
    switch (key) {
        case 'ArrowLeft':
            return nearestInRow(cells, from, -1);
        case 'ArrowRight':
            return nearestInRow(cells, from, 1);
        case 'ArrowUp':
            return nearestInColumn(cells, from, -1);
        case 'ArrowDown':
            return nearestInColumn(cells, from, 1);
        case 'Home':
            return edgeOfRow(cells, from, -1);
        case 'End':
            return edgeOfRow(cells, from, 1);
    }
}

function nearestInRow<T extends GridPosition>(cells: readonly T[], from: T, direction: 1 | -1): T | null {
    const candidates = cells.filter((cell) => cell.row === from.row && Math.sign(cell.column - from.column) === direction);

    return candidates.reduce<T | null>((best, cell) =>
        best === null || Math.abs(cell.column - from.column) < Math.abs(best.column - from.column) ? cell : best, null);
}

function nearestInColumn<T extends GridPosition>(cells: readonly T[], from: T, direction: 1 | -1): T | null {
    const candidates = cells.filter((cell) => cell.column === from.column && Math.sign(cell.row - from.row) === direction);

    return candidates.reduce<T | null>((best, cell) =>
        best === null || Math.abs(cell.row - from.row) < Math.abs(best.row - from.row) ? cell : best, null);
}

function edgeOfRow<T extends GridPosition>(cells: readonly T[], from: T, direction: 1 | -1): T | null {
    const row = cells.filter((cell) => cell.row === from.row);

    return row.reduce<T | null>((best, cell) =>
        best === null || Math.sign(cell.column - best.column) === direction ? cell : best, null);
}