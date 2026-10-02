import { PhaseSpanSeconds } from "./reaction-script";

export function easeInOutCubic(progress: number): number {
    return progress < 0.5 ? 4 * Math.pow(progress, 3) : 1 - Math.pow(-2 * progress + 2, 3) / 2;
}

export function easeInOutCubicRateOf(progress: number): number {
    return progress < 0.5 ? 12 * progress * progress : 12 * (1 - progress) * (1 - progress);
}

export function easeInToGlide(progress: number): number {
    return progress * progress * (2.75 - 1.75 * progress);
}

export function glideRateOf(progress: number): number {
    return progress * (5.5 - 5.25 * progress);
}

export function progressBetween(startSeconds: number, endSeconds: number, seconds: number): number {
    if (endSeconds <= startSeconds) {
        return seconds >= endSeconds ? 1 : 0;
    }

    return Math.min(Math.max((seconds - startSeconds) / (endSeconds - startSeconds), 0), 1);
}

export function smoothProgressBetween(startSeconds: number, endSeconds: number, seconds: number): number {
    const progress = progressBetween(startSeconds, endSeconds, seconds);

    return progress * progress * (3 - 2 * progress);
}

export function progressWithin(span: PhaseSpanSeconds, seconds: number): number {
    return progressBetween(span.start, span.end, seconds);
}