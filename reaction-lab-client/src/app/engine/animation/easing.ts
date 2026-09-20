import { PhaseSpanSeconds } from "./reaction-script";

export function easeInOutCubic(progress: number): number {
    return progress < 0.5 ? 4 * Math.pow(progress, 3) : 1 - Math.pow(-2 * progress + 2, 3) / 2;
}

export function easeOutCubic(progress: number): number {
    return 1 - Math.pow(1 - progress, 3);
}

export function easeOutPower(progress: number, exponent: number): number {
    return 1 - Math.pow(1 - progress, exponent);
}

export function progressBetween(startSeconds: number, endSeconds: number, seconds: number): number {
    if (endSeconds <= startSeconds) {
        return seconds >= endSeconds ? 1 : 0;
    }

    return Math.min(Math.max((seconds - startSeconds) / (endSeconds - startSeconds), 0), 1);
}

export function progressWithin(span: PhaseSpanSeconds, seconds: number): number {
    return progressBetween(span.start, span.end, seconds);
}