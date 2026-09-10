export function prefersReducedMotion(view: Window): boolean {
    return typeof view.matchMedia === 'function' && view.matchMedia('(prefers-reduced-motion: reduce)').matches;
}