export class HighlightFade {
    private readonly current = new Map<string, number>();

    private targetLevels: ReadonlyMap<string, number> = new Map();
    private reducedMotion = false;

    constructor(
        private readonly riseSeconds: number,
        private readonly fallSeconds: number
    ) {}

    get highlightLevels(): ReadonlyMap<string, number> {
        return this.current;
    }

    setReducedMotion(enabled: boolean): void {
        this.reducedMotion = enabled;
    }

    setTargetLevels(targetLevels: ReadonlyMap<string, number>): void {
        this.targetLevels = targetLevels;
    }

    update(delta: number): boolean {
        let moving = false;

        for (const [id, target] of this.targetLevels) {
            moving = this.step(id, target, delta) || moving;
        }

        for (const id of this.current.keys()) {
            if (!this.targetLevels.has(id)) {
                moving = this.step(id, 0, delta) || moving;
            }
        }

        return moving;
    }

    private step(id: string, target: number, delta: number): boolean {
        const value = this.current.get(id) ?? 0;

        if (value === target) {
            return false;
        }

        const seconds = target > value ? this.riseSeconds : this.fallSeconds;
        const stride = this.reducedMotion ? 1 : delta / seconds;
        const next = value < target ? Math.min(target, value + stride) : Math.max(target, value - stride);

        if (next === 0) {
            this.current.delete(id);
        } else {
            this.current.set(id, next);
        }

        return true;
    }
}