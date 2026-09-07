import { Disposable } from "./disposal-scope";
import { EngineContext } from "./engine-context";

export type ResizeListener = (width: number, height: number) => void;

const MAX_PIXEL_RATIO = 2;

export class ViewportObserver implements Disposable {
    private readonly observer: ResizeObserver;
    private readonly listeners = new Set<ResizeListener>();

    constructor(
        host: HTMLElement,
        private readonly context: EngineContext,
        private readonly view: Window
    ) {
        this.observer = new ResizeObserver((entries) => this.resize(entries[0]));

        try {
            this.observer.observe(host, { box: 'device-pixel-content-box' });
        } catch {
            this.observer.observe(host);
        }
    }

    onResize(listener: ResizeListener): () => void {
        this.listeners.add(listener);

        return () => this.listeners.delete(listener);
    }

    dispose(): void {
        this.observer.disconnect();
        this.listeners.clear();
    }

    private resize(entry: ResizeObserverEntry): void {
        const { width, height } = entry.contentRect;
        const pixels = entry.devicePixelContentBoxSize?.[0];
        const ratio = this.view.devicePixelRatio;
        const scale = Math.min(1, MAX_PIXEL_RATIO / ratio);

        if (pixels) {
            this.context.renderer.setPixelRatio(1);
            this.context.renderer.setSize(Math.round(pixels.inlineSize * scale), Math.round(pixels.blockSize * scale), false);
        } else {
            this.context.renderer.setPixelRatio(ratio * scale);
            this.context.renderer.setSize(width, height, false);
        }

        if (height > 0) {
            this.context.camera.aspect = width / height;
            this.context.camera.updateProjectionMatrix();
        }

        for (const listener of this.listeners) {
            listener(width, height);
        }

        if (width > 0 && height > 0) {
            this.context.render();
        }
    }
}