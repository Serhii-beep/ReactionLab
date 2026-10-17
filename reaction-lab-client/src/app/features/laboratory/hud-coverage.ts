import { DestroyRef, ElementRef, inject } from "@angular/core";
import { SceneViewport } from "./scene/scene-viewport";

export function reportHudCoverage(): void {
    const host = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
    const viewport = inject(SceneViewport);
    const observer = new ResizeObserver(() => viewport.setBenchCoveredPixels(coveredPixelsOf(host)));

    observer.observe(host);
    inject(DestroyRef).onDestroy(() => {
        observer.disconnect();
        viewport.setBenchCoveredPixels(0);
    });
}

function coveredPixelsOf(host: HTMLElement): number {
    const container = host.offsetParent;

    return container instanceof HTMLElement ? container.clientHeight - host.offsetTop : 0;
}
