export class PlaybackClock {
    private duration = 0;
    private elapsed = 0;

    get durationSeconds(): number {
        return this.duration;
    }

    get elapsedSeconds(): number {
        return this.elapsed;
    }

    get atEnd(): boolean {
        return this.elapsed >= this.duration;
    }

    reset(durationSeconds: number): void {
        this.duration = Math.max(0, durationSeconds);
        this.elapsed = 0;
    }

    advance(stepSeconds: number): void {
        this.seek(this.elapsed + stepSeconds);
    }

    seek(seconds: number): void {
        this.elapsed = Math.min(Math.max(seconds, 0), this.duration);
    }
}