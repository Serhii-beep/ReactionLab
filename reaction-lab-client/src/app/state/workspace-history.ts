export interface History<T> {
    readonly past: readonly T[];
    readonly present: T;
    readonly future: readonly T[];
}

export const HISTORY_LIMIT = 50;

export function initial<T>(present: T): History<T> {
    return { past: [], present, future: [] };
}

export function commit<T>(history: History<T>, next: T): History<T> {
    if (next === history.present) {
        return history;
    }

    const past = [...history.past, history.present];

    return {
        past: past.length > HISTORY_LIMIT ? past.slice(past.length - HISTORY_LIMIT) : past,
        present: next,
        future: []
    };
}

export function undo<T>(history: History<T>): History<T> {
    if (history.past.length === 0) {
        return history;
    }

    return {
        past: history.past.slice(0, -1),
        present: history.past[history.past.length - 1],
        future: [history.present, ...history.future]
    };
}

export function redo<T>(history: History<T>): History<T> {
    if (history.future.length === 0) {
        return history;
    }

    return {
        past: [...history.past, history.present],
        present: history.future[0],
        future: history.future.slice(1)
    };
}

export function canUndo<T>(history: History<T>): boolean {
    return history.past.length > 0;
}

export function canRedo<T>(history: History<T>): boolean {
    return history.future.length > 0;
}