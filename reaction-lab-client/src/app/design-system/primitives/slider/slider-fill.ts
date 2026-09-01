export function fillPercent(value: number, min: number, max: number): number {
    if (max <= min) {
        return 0;
    }

    const ratio = (value - min) / (max - min);

    return Math.min(100, Math.max(0, ratio * 100));
}