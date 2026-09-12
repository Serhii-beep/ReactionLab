import { Disposable } from "./disposal-scope";
import { EngineContext } from "./engine-context";

export type UpdateListener = (stepSeconds: number) => void;
export type RenderListener = (deltaSeconds: number) => boolean;
export type PresentListener = (intervalSeconds: number) => void;

const FIXED_STEP = 1 / 60;
const MAX_FRAME = 0.25;
const MAX_STEPS = 5;

export class RenderLoop implements Disposable {
    private readonly updates = new Set<UpdateListener>();
    private readonly renders = new Set<RenderListener>();
    private readonly presents = new Set<PresentListener>();

    private accumulator = 0;
    private last: number | null = null;
    private running = false;
    private suspended = false;
    private invalidated = true;
    private presentedLastTick = false;
    private presentedFrames = 0;

    constructor(
        private readonly context: EngineContext,
        private readonly document: Document
    ) {}

    get frames(): number {
        return this.presentedFrames;
    }

    onUpdate(listener: UpdateListener): () => void {
        this.updates.add(listener);

        return () => this.updates.delete(listener);
    }

    onRender(listener: RenderListener): () => void {
        this.renders.add(listener);

        return () => this.renders.delete(listener);
    }

    onPresent(listener: PresentListener): () => void {
        this.presents.add(listener);

        return () => this.presents.delete(listener);
    }

    invalidate(): void {
        this.invalidated = true;
    }

    start(): void {
        this.running = true;
        this.document.addEventListener('visibilitychange', this.onVisibility);
        this.apply();
    }

    stop(): void {
        this.running = false;
        this.document.removeEventListener('visibilitychange', this.onVisibility);
        this.apply();
    }

    suspend(suspended: boolean): void {
        this.suspended = suspended;
        this.apply();
    }

    dispose(): void {
        this.stop();
        this.updates.clear();
        this.renders.clear();
        this.presents.clear();
    }

    private apply(): void {
        const active = this.running && !this.suspended && this.document.visibilityState === 'visible';

        this.context.renderer.setAnimationLoop(active ? this.tick : null);

        if (!active) {
            this.last = null;
            this.presentedLastTick = false;
        }
    }

    private readonly onVisibility = (): void => this.apply();

    private readonly tick = (time: number): void => {
        const seconds = time / 1000;
        const delta = this.last === null ? 0 : Math.min(seconds - this.last, MAX_FRAME);

        this.last = seconds;
        this.accumulator += delta;

        let steps = 0;

        while (this.accumulator >= FIXED_STEP && steps < MAX_STEPS) {
            for (const update of this.updates) {
                update(FIXED_STEP);
            }

            this.accumulator -= FIXED_STEP;
            steps++;
        }

        if (steps === MAX_STEPS) {
            this.accumulator = 0;
        }

        let mustPresent = this.invalidated;

        this.invalidated = false;

        for (const render of this.renders) {
            mustPresent = render(delta) || mustPresent;
        }

        if (mustPresent) {
            this.presentFrame(delta);
        }

        this.presentedLastTick = mustPresent;
    }

    private presentFrame(deltaSeconds: number): void {
        this.context.renderer.info.reset();
        this.context.render();
        this.presentedFrames++;

        if (this.presentedLastTick) {
            for (const listener of this.presents) {
                listener(deltaSeconds);
            }
        }
    }
}