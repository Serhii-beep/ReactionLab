import { Group, Vector3 } from "three";
import { Disposable } from "../core/disposal-scope";
import { QualityTier } from "../rendering/quality";
import { ParticleEmitter } from "./particle-emitter";
import { EmissionAnchors, EmissionPlan } from "./emission-plan";
import { QualityLevel } from "../performance/quality-governor";
import { ParticleKind } from "./particle-look";

const PARTICLE_BUDGET_BY_TIER: Readonly<Record<QualityTier, number>> = { high: 1, medium: 0.6, low: 0.35 };
const PARTICLE_KINDS: readonly ParticleKind[] = ['sparks', 'flame', 'smoke', 'precipitate'];
const KEEPER_ANCHORS: EmissionAnchors = { meeting: new Vector3(), products: [], reactants: [] };

export class ReactionEffects implements Disposable {
    readonly root = new Group();

    private readonly keepers: readonly ParticleEmitter[];
    private emitters: ParticleEmitter[] = [];
    private plans: readonly EmissionPlan[] = [];
    private anchors: EmissionAnchors | null = null;
    private budgetScale = PARTICLE_BUDGET_BY_TIER.high;
    private elapsedSeconds = 0;
    private shownSeconds: number | null = null;

    constructor() {
        this.root.name = 'reaction-effects';
        this.keepers = PARTICLE_KINDS.map((kind) => new ParticleEmitter(keeperPlanFor(kind), KEEPER_ANCHORS, 1));

        for (const keeper of this.keepers) {
            this.root.add(keeper.mesh);
        }
    }

    begin(plans: readonly EmissionPlan[], anchors: EmissionAnchors): void {
        this.plans = plans;
        this.anchors = anchors;
        this.elapsedSeconds = 0;
        this.shownSeconds = null;
        this.rebuild();
    }

    setElapsed(elapsedSeconds: number): boolean {
        const moved = elapsedSeconds !== this.shownSeconds;
        let alive = false;

        this.elapsedSeconds = elapsedSeconds;
        this.shownSeconds = elapsedSeconds;

        for (const emitter of this.emitters) {
            alive = emitter.setElapsed(elapsedSeconds) || alive;
        }

        return alive && moved;
    }

    setQuality(level: QualityLevel): void {
        const budgetScale = PARTICLE_BUDGET_BY_TIER[level.tier] * level.resolutionScale;

        if (budgetScale !== this.budgetScale) {
            this.budgetScale = budgetScale;
            this.rebuild();
        }
    }

    end(): void {
        this.plans = [];
        this.anchors = null;
        this.rebuild();
    }

    dispose(): void {
        disposeEmitters(this.emitters);
        disposeEmitters(this.keepers);
        this.emitters = [];
        this.root.removeFromParent();
    }

    private rebuild(): void {
        const anchors = this.anchors;
        
        disposeEmitters(this.emitters);
        this.emitters = anchors === null ? [] : this.plans.map((plan) => this.buildEmitter(plan, anchors));
    }

    private buildEmitter(plan: EmissionPlan, anchors: EmissionAnchors): ParticleEmitter {
        const emitter = new ParticleEmitter(plan, anchors, this.budgetScale);

        emitter.setElapsed(this.elapsedSeconds);
        this.root.add(emitter.mesh);

        return emitter;
    }
}

function keeperPlanFor(kind: ParticleKind): EmissionPlan {
    return {
        kind,
        anchor: 'meeting',
        startSeconds: -1,
        windowSeconds: 0,
        lifeSeconds: 0.001,
        particleCount: 1,
        spreadAngstrom: 0,
        magnitude: 1,
        colorStart: 0,
        colorEnd: 0,
        intensity: 0,
        opacity: 0
    };
}

function disposeEmitters(emitters: readonly ParticleEmitter[]): void {
    for (const emitter of emitters) {
        emitter.dispose();
    }
}