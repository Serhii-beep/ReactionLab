import { Vector3 } from "three";

export type ParticleKind = 'flame' | 'smoke';

export interface ParticleLook {
    readonly additive: boolean;
    readonly gravityAngstromPerSecondSquared: Vector3;
    readonly dragPerSecond: number;
    readonly growthPerLife: number;
    readonly fadeInLifeFraction: number;
    readonly fadeOutLifeFraction: number;
    readonly speedAngstromPerSecond: number;
    readonly liftAngstromPerSecond: number;
    readonly sizeAngstrom: number;
}

export const PARTICLE_LOOKS: Readonly<Record<ParticleKind, ParticleLook>> = {
    flame: {
        additive: true,
        gravityAngstromPerSecondSquared: new Vector3(0, 0, 0),
        dragPerSecond: 1.5,
        growthPerLife: 1,
        fadeInLifeFraction: 0.15,
        fadeOutLifeFraction: 0.5,
        speedAngstromPerSecond: 0.3,
        liftAngstromPerSecond: 1.6,
        sizeAngstrom: 0.5
    },
    smoke: {
        additive: false,
        gravityAngstromPerSecondSquared: new Vector3(0, 0.1, 0),
        dragPerSecond: 1,
        growthPerLife: 2.5,
        fadeInLifeFraction: 0.2,
        fadeOutLifeFraction: 0.5,
        speedAngstromPerSecond: 0.25,
        liftAngstromPerSecond: 0.9,
        sizeAngstrom: 0.6
    },
}