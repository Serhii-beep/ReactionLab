import { DestroyRef, inject, Injectable } from "@angular/core";
import { ReactionRun } from "./reaction-run";
import { ReactionDirector } from "../../../engine/animation/reaction-director";
import { BenchScene } from "../../../engine/scene/bench-scene";
import { RenderLoop } from "../../../engine/core/render-loop";
import { ReactionScript } from "../../../engine/animation/reaction-script";

@Injectable()
export class RunConductor {
    private readonly run = inject(ReactionRun);
    private readonly director = inject(ReactionDirector);
    private readonly scene = inject(BenchScene);
    private readonly loop = inject(RenderLoop);
    private readonly destroyRef = inject(DestroyRef);

    bind(animated: () => boolean): void {
        this.destroyRef.onDestroy(this.run.attach({
            start: (script: ReactionScript) => {
                this.director.start(script);
                this.scene.beginRun(script, animated());
            },
            pause: () => this.director.pause(),
            resume: () => this.director.resume(),
            seek: (seconds: number) => this.director.seek(seconds),
            stop: () => {
                this.director.stop();
                this.scene.endRun();
            }
        }));
        this.destroyRef.onDestroy(this.loop.onUpdate((stepSeconds) => this.director.advance(stepSeconds)));
        this.destroyRef.onDestroy(this.loop.onRender(() => {
            this.run.tick(this.director.elapsedSeconds);

            return false;
        }));
        this.destroyRef.onDestroy(this.director.onFinished(() => this.run.markFinished()));
    }
}