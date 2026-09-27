import { Vector3 } from "three";
import { ParticleKind } from "./particle-look";

export type EmissionAnchor = 'meeting' | 'products' | 'reactants';

export interface EmissionPlan {
    readonly kind: ParticleKind;
    readonly anchor: EmissionAnchor;
    readonly startSeconds: number;
    readonly windowSeconds: number;
    readonly lifeSeconds: number;
    readonly particleCount: number;
    readonly spreadAngstrom: number;
    readonly magnitude: number;
    readonly colorStart: number;
    readonly colorEnd: number;
    readonly intensity: number;
    readonly opacity: number;
}

export interface EmissionAnchors {
    readonly meeting: Vector3;
    readonly products: readonly Vector3[];
    readonly reactants: readonly Vector3[];
}

export function anchorPointsOf(anchors: EmissionAnchors, anchor: EmissionAnchor): readonly Vector3[] {
    switch (anchor) {
        case 'meeting':
            return [anchors.meeting];
        case 'products':
            return anchors.products.length > 0 ? anchors.products : [anchors.meeting];
        case "reactants":
            return anchors.reactants.length > 0 ? anchors.reactants : [anchors.meeting];
    }
}

export function hash01(index: number, channel: number): number {
    const value = Math.sin(index * 12.9898 + channel * 78.233) * 43758.5453;

    return value - Math.floor(value);
}