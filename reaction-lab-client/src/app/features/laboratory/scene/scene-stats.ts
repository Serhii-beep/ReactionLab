import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from "@angular/core";
import { RenderLoop } from "../../../engine/core/render-loop";
import { QualityGovernor } from "../../../engine/performance/quality-governor";
import { EngineContext } from "../../../engine/core/engine-context";

interface SceneStatsSnapshot {
    readonly framesPerSecond: number;
    readonly frameMilliseconds: number;
    readonly qualityLevel: string;
    readonly drawCalls: number;
    readonly triangles: number;
    readonly geometries: number;
    readonly textures: number;
}

const REFRESH_MS = 500;

@Component({
    selector: 'app-scene-stats',
    templateUrl: './scene-stats.html',
    styleUrl: './scene-stats.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class SceneStats {
    protected readonly snapshot = signal<SceneStatsSnapshot>({
        framesPerSecond: 0, frameMilliseconds: 0, qualityLevel: '', drawCalls: 0, triangles: 0, geometries: 0, textures: 0
    });

    private readonly loop = inject(RenderLoop);
    private readonly governor = inject(QualityGovernor);
    private readonly context = inject(EngineContext);

    constructor() {
        let framesAtLastRefresh = this.loop.frames;
        let lastRefresh = performance.now();
        const timer = setInterval(() => {
            const now = performance.now();
            const frames = this.loop.frames;
            const { render, memory } = this.context.renderer.info;

            this.snapshot.set({
                framesPerSecond: Math.round(((frames - framesAtLastRefresh) * 1000) / (now - lastRefresh)),
                frameMilliseconds: Math.round(this.governor.lastInterval * 10000) / 10,
                qualityLevel: this.governor.level.name,
                drawCalls: render.calls,
                triangles: render.triangles,
                geometries: memory.geometries,
                textures: memory.textures
            });
            framesAtLastRefresh = frames;
            lastRefresh = now;
        }, REFRESH_MS);

        inject(DestroyRef).onDestroy(() => clearInterval(timer));
    }
}