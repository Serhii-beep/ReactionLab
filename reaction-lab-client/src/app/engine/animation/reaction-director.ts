import { PlaybackClock } from "./playback-clock";
import { ReactionScript } from "./reaction-script";

export type DirectorStatus = 'idle' | 'playing' | 'paused' | 'finished';

export class ReactionDirector {
    private readonly clock = new PlaybackClock();
    private readonly finishedListeners = new Set<() => void>();
    private runningScript: ReactionScript | null = null;
    private currentStatus: DirectorStatus = 'idle';
    private movedSinceRender = false;

    get script(): ReactionScript | null {
        return this.runningScript;
    }

    get status(): DirectorStatus {
        return this.currentStatus;
    }

    get elapsedSeconds(): number {
        return this.clock.elapsedSeconds;
    }

    onFinished(listener: () => void): () => void {
        this.finishedListeners.add(listener);

        return () => this.finishedListeners.delete(listener);
    }

    start(script: ReactionScript): void {
        this.runningScript = script;
        this.clock.reset(script.durationSeconds);
        this.currentStatus = 'playing';
        this.movedSinceRender = true;
    }

    pause(): void {
        if (this.currentStatus === 'playing') {
            this.currentStatus = 'paused';
        }
    }

    resume(): void {
        if (this.currentStatus === 'paused') {
            this.currentStatus = 'playing';
        }
    }

    seek(seconds: number): void {
        if (this.runningScript === null) {
            return;
        }

        this.clock.seek(seconds);
        this.movedSinceRender = true;

        if (this.clock.atEnd) {
            this.finish();
        } else if (this.currentStatus === 'finished') {
            this.currentStatus = 'paused';
        }
    }

    advance(stepSeconds: number): void {
        if (this.currentStatus !== 'playing') {
            return;
        }

        this.clock.advance(stepSeconds);
        this.movedSinceRender = true;

        if (this.clock.atEnd) {
            this.finish();
        }
    }

    stop(): void {
        this.runningScript = null;
        this.currentStatus = 'idle';
        this.clock.reset(0);
        this.movedSinceRender = true;
    }

    consumeMovement(): boolean {
        const moved = this.movedSinceRender;

        this.movedSinceRender = false;

        return moved;
    }

    private finish(): void {
        if (this.currentStatus === 'finished') {
            return;
        }

        this.currentStatus = 'finished';

        for (const listener of this.finishedListeners) {
            listener();
        }
    }
}