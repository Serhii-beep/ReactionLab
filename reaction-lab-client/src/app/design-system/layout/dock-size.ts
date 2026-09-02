export interface DockBounds {
    readonly min: number;
    readonly max: number;
    readonly collapseAt: number;
}

export interface DockState {
    readonly size: number;
    readonly collapsed: boolean;
}

export const COLLAPSE_AT = 0.6;

export function clampSize(size: number, bounds: DockBounds): number {
    return Math.min(bounds.max, Math.max(bounds.min, size));
}

export function resolveDrag(size: number, current: DockState, bounds: DockBounds): DockState {
    if (size < bounds.min * bounds.collapseAt) {
        return { size: current.size, collapsed: true };
    }

    return { size: clampSize(size, bounds), collapsed: false };
}

export function parseDockState(raw: string | null, bounds: DockBounds, fallback: number): DockState {
    const size = clampSize(fallback, bounds);

    if (raw === null) {
        return { size, collapsed: false };
    }

    try {
        const stored: unknown = JSON.parse(raw);

        if (typeof stored !== 'object' || stored === null) {
            return { size, collapsed: false };
        }

        const candidate = stored as Partial<DockState>;

        return { size: storedSize(candidate.size, bounds, size), collapsed: candidate.collapsed === true };
    } catch {
        return { size, collapsed: false };
    }
}

function storedSize(value: unknown, bounds: DockBounds, fallback: number): number {
    return typeof value === 'number' && Number.isFinite(value) ? clampSize(value, bounds) : fallback;
}