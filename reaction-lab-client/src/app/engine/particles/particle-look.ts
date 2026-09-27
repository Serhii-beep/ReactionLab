import { Vector3 } from "three";

export type ParticleKind = 'sparks' | 'flame' | 'smoke' | 'precipitate';

export interface ParticleLook {
    readonly opaque: boolean;
    readonly additive: boolean;
    readonly gravityAngstromPerSecondSquared: Vector3;
    readonly dragPerSecond: number;
    readonly growthPerLife: number;
    readonly fadeInLifeFraction: number;
    readonly fadeOutLifeFraction: number;
    readonly settlesOnFloor: boolean;
    readonly speedAngstromPerSecond: number;
    readonly liftAngstromPerSecond: number;
    readonly sizeAngstrom: number;
}

export const PARTICLE_LOOKS: Readonly<Record<ParticleKind, ParticleLook>> = {
    sparks: {
        opaque: false,
        additive: true,
        gravityAngstromPerSecondSquared: new Vector3(0, -6, 0),
        dragPerSecond: 2.5,
        growthPerLife: -0.6,
        fadeInLifeFraction: 0.1,
        fadeOutLifeFraction: 0.45,
        settlesOnFloor: false,
        speedAngstromPerSecond: 5,
        liftAngstromPerSecond: 1.5,
        sizeAngstrom: 0.14
    },
    flame: {
        opaque: false,
        additive: true,
        gravityAngstromPerSecondSquared: new Vector3(0, 0, 0),
        dragPerSecond: 1.5,
        growthPerLife: 1,
        fadeInLifeFraction: 0.15,
        fadeOutLifeFraction: 0.5,
        settlesOnFloor: false,
        speedAngstromPerSecond: 0.3,
        liftAngstromPerSecond: 1.6,
        sizeAngstrom: 0.5
    },
    smoke: {
        opaque: false,
        additive: false,
        gravityAngstromPerSecondSquared: new Vector3(0, 0.1, 0),
        dragPerSecond: 1,
        growthPerLife: 2.5,
        fadeInLifeFraction: 0.2,
        fadeOutLifeFraction: 0.5,
        settlesOnFloor: false,
        speedAngstromPerSecond: 0.25,
        liftAngstromPerSecond: 0.9,
        sizeAngstrom: 0.6
    },
    precipitate: {
        opaque: true,
        additive: false,
        gravityAngstromPerSecondSquared: new Vector3(0, -9, 0),
        dragPerSecond: 1,
        growthPerLife: 0,
        fadeInLifeFraction: 0.05,
        fadeOutLifeFraction: 0.2,
        settlesOnFloor: true,
        speedAngstromPerSecond: 0.5,
        liftAngstromPerSecond: 0.3,
        sizeAngstrom: 0.11
    },
}