export interface StepBounds {
    readonly min?: number;
    readonly max?: number;
    readonly step: number;
}

export function decimalsOf(step: number): number {
    const text = String(step);
    const dot = text.indexOf('.');

    return dot < 0 ? 0 : text.length - dot - 1;
}

export function round(value: number, decimals: number): number {
    const factor = 10 ** decimals;

    return Math.round(value * factor) / factor;
}

export function clamp(value: number, bounds: StepBounds): number {
    const { min, max } = bounds;
    let result = value;

    if (min !== undefined) {
        result = Math.max(min, result);
    }

    if (max !== undefined) {
        result = Math.min(max, result);
    }

    return result;
}

export function snap(value: number, bounds: StepBounds): number {
    const anchor = bounds.min ?? 0;
    const steps = Math.round((value - anchor) / bounds.step);

    return clamp(round(anchor + steps * bounds.step, decimalsOf(bounds.step)), bounds);
}

export function stepBy(value: number, multiplier: number, bounds: StepBounds): number {
    const anchor = bounds.min ?? 0;
    const steps = Math.round((value - anchor) / bounds.step);

    return clamp(round(anchor + (steps + multiplier) * bounds.step, decimalsOf(bounds.step)), bounds);
}