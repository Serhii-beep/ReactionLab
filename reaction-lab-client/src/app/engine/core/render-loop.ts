import { Disposable } from "./disposal-scope";
import { EngineContext } from "./engine-context";

export type FrameListener = (seconds: number) => void;

const FIXED_STEP = 1 / 60;
const MAX_FRAME = 0.25;
const MAX_STEPS = 5;

export class RenderLoop implements Disposable {
    private readonly updates = new Set<FrameListener>();
    private readonly renders = new Set<FrameListener>();
    private accumulator = 0;
    private last: number | null = null;
    private running = false;
    private suspended = false;
    private frame = 0;

    constructor(
        private readonly context: EngineContext,
        private readonly document: Document
    ) {}

    get frames(): number {
        return this.frame;
    }

    onUpdate(listener: FrameListener): () => void {
        this.updates.add(listener);

        return () => this.updates.delete(listener);
    }

    onRender(listener: FrameListener): () => void {
        this.renders.add(listener);

        return () => this.renders.delete(listener);
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
    }

    private apply(): void {
        const active = this.running && !this.suspended && this.document.visibilityState === 'visible';

        this.context.renderer.setAnimationLoop(active ? this.tick : null);

        if (!active) {
            this.last = null;
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

        for (const render of this.renders) {
            render(delta);
        }

        this.context.render();
        this.frame++;
    }
}