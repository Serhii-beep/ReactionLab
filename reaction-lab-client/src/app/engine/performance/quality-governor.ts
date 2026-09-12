import { ViewportObserver } from "../core/viewport-observer";
import { PostProcessingPipeline } from "../rendering/post-processing-pipeline";
import { QualityTier } from "../rendering/quality";
import { Lod } from "../resources/geometry-cache";
import { LodController } from "./lod-controller";

export interface QualityLevel {
    readonly name: string;
    readonly tier: QualityTier;
    readonly resolutionScale: number;
    readonly lodCeiling: Lod;
}

export type QualityListener = (level: QualityLevel) => void;

export const QUALITY_LADDER: readonly QualityLevel[] = [
    { name: 'high', tier: 'high', resolutionScale: 1, lodCeiling: 'high' },
    { name: 'high 80%', tier: 'high', resolutionScale: 0.8, lodCeiling: 'high' },
    { name: 'medium', tier: 'medium', resolutionScale: 1, lodCeiling: 'high' },
    { name: 'medium 75%', tier: 'medium', resolutionScale: 0.75, lodCeiling: 'medium' },
    { name: 'low', tier: 'low', resolutionScale: 1, lodCeiling: 'medium' },
    { name: 'low 75%', tier: 'low', resolutionScale: 0.75, lodCeiling: 'medium' },
    { name: 'low 50%', tier: 'low', resolutionScale: 0.5, lodCeiling: 'low' }
];

const TARGET_INTERVAL_SECONDS = 1 / 60;
const MISSED_FACTOR = 1.5;
const WINDOW_SECONDS = 1;
const MIN_FRAMES_PER_WINDOW = 20;
const STEP_DOWN_MISSED_RATIO = 0.2;
const STEP_UP_MISSED_RATIO = 0.02;
const INITIAL_PATIENCE_WINDOWS = 5;
const MAX_PATIENCE_WINDOWS = 60;
const COOLDOWN_WINDOWS = 2;

export class QualityGovernor {
    private readonly listeners = new Set<QualityListener>();

    private levelIndex = 0;
    private windowSeconds = 0;
    private windowFrames = 0;
    private windowMissed = 0;
    private calmWindows = 0;
    private cooldownWindows = 0;
    private patienceWindows = INITIAL_PATIENCE_WINDOWS;
    private lastIntervalSeconds = 0;
    private adapting = true;

    constructor(
        private readonly pipeline: PostProcessingPipeline,
        private readonly viewport: ViewportObserver,
        private readonly lod: LodController
    ) {}

    get level(): QualityLevel {
        return QUALITY_LADDER[this.levelIndex];
    }

    get lastInterval(): number {
        return this.lastIntervalSeconds;
    }

    onChange(listener: QualityListener): () => void {
        this.listeners.add(listener);

        return () => this.listeners.delete(listener);
    }

    apply(index: number): void {
        const clamped = Math.min(Math.max(index, 0), QUALITY_LADDER.length - 1);

        this.levelIndex = clamped;
        this.pipeline.setQuality(this.level.tier);
        this.viewport.setResolutionScale(this.level.resolutionScale);
        this.lod.setCeiling(this.level.lodCeiling);
        this.resetWindow();
        this.cooldownWindows = COOLDOWN_WINDOWS;

        for (const listener of this.listeners) {
            listener(this.level);
        }
    }

    pin(index: number): void {
        this.adapting = false;
        this.apply(index);
    }

    sample(intervalSeconds: number): void {
        if (intervalSeconds <= 0) {
            return;
        }

        this.lastIntervalSeconds = intervalSeconds;
        this.windowSeconds += intervalSeconds;
        this.windowFrames++;

        if (intervalSeconds > TARGET_INTERVAL_SECONDS * MISSED_FACTOR) {
            this.windowMissed++;
        }

        if (this.windowSeconds >= WINDOW_SECONDS) {
            this.evaluate();
        }
    }

    private evaluate(): void {
        const { windowFrames, windowMissed } = this;

        this.resetWindow();

        if (!this.adapting || windowFrames < MIN_FRAMES_PER_WINDOW) {
            return;
        }

        if (this.cooldownWindows > 0) {
            this.cooldownWindows--;

            return;
        }

        const missedRatio = windowMissed / windowFrames;

        if (missedRatio > STEP_DOWN_MISSED_RATIO) {
            this.stepDown();
        } else if (missedRatio < STEP_UP_MISSED_RATIO) {
            this.stepUpWhenPatient();
        } else {
            this.calmWindows = 0;
        }
    }

    private stepDown(): void {
        this.calmWindows = 0;
        this.patienceWindows = Math.min(this.patienceWindows * 2, MAX_PATIENCE_WINDOWS);

        if (this.levelIndex < QUALITY_LADDER.length - 1) {
            this.apply(this.levelIndex + 1);
        }
    }

    private stepUpWhenPatient(): void {
        this.calmWindows++;

        if (this.calmWindows >= this.patienceWindows && this.levelIndex > 0) {
            this.calmWindows = 0;
            this.apply(this.levelIndex - 1);
            this.patienceWindows = Math.max(INITIAL_PATIENCE_WINDOWS, this.patienceWindows / 2);
        }
    }

    private resetWindow(): void {
        this.windowSeconds = 0;
        this.windowFrames = 0;
        this.windowMissed = 0;
    }
}