import { Disposable } from "./disposal-scope";
import { EngineContext } from "./engine-context";
import { RenderLoop } from "./render-loop";

export type ContextListener = () => void;

export class ContextGuard implements Disposable {
    private readonly lostListeners = new Set<ContextListener>();
    private readonly restoredListeners = new Set<ContextListener>();

    constructor(
        private readonly context: EngineContext,
        private readonly loop: RenderLoop
    ) {
        context.canvas.addEventListener('webglcontextlost', this.handleContextLost);
        context.canvas.addEventListener('webglcontextrestored', this.handleContextRestored);
    }

    onLost(listener: ContextListener): () => void {
        this.lostListeners.add(listener);

        return () => this.lostListeners.delete(listener);
    }

    onRestored(listener: ContextListener): () => void {
        this.restoredListeners.add(listener);

        return () => this.restoredListeners.delete(listener);
    }

    dispose(): void {
        this.context.canvas.removeEventListener('webglcontextlost', this.handleContextLost);
        this.context.canvas.removeEventListener('webglcontextrestored', this.handleContextRestored);
        this.lostListeners.clear();
        this.restoredListeners.clear();
    }

    private readonly handleContextLost: EventListener = (event): void => {
        event.preventDefault();
        this.loop.suspend(true);

        for (const listener of this.lostListeners) {
            listener();
        }
    };

    private readonly handleContextRestored: EventListener = (): void => {
        this.loop.suspend(false);
        this.loop.invalidate();

        for (const listener of this.restoredListeners) {
            listener();
        }
    };
}