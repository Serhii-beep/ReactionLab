export type SparkMoment = 'contact' | 'snap' | 'burst' | 'ember';

export interface ValueSpread {
    readonly minimum: number;
    readonly maximum: number;
}

export interface SparkStyle {
    readonly countAtFullHeat: number;
    readonly delaySeconds: ValueSpread;
    readonly speedAngstromPerSecond: ValueSpread;
    readonly dragPerSecond: number;
    readonly liftAngstromPerSecondSquared: number;
    readonly coolingSeconds: ValueSpread;
    readonly widthPixels: ValueSpread;
}

export const SPARK_STYLES: Readonly<Record<SparkMoment, SparkStyle>> = {
    contact: {
        countAtFullHeat: 10,
        delaySeconds: { minimum: 0, maximum: 0.04 },
        speedAngstromPerSecond: { minimum: 1.5, maximum: 4 },
        dragPerSecond: 1.8,
        liftAngstromPerSecondSquared: -2,
        coolingSeconds: { minimum: 0.15, maximum: 0.25 },
        widthPixels: { minimum: 1.2, maximum: 2.2 }
    },
    snap: {
        countAtFullHeat: 6,
        delaySeconds: { minimum: 0, maximum: 0.03 },
        speedAngstromPerSecond: { minimum: 2, maximum: 4.5 },
        dragPerSecond: 1.6,
        liftAngstromPerSecondSquared: -2.5,
        coolingSeconds: { minimum: 0.18, maximum: 0.3 },
        widthPixels: { minimum: 1.2, maximum: 2 }
    },
    burst: {
        countAtFullHeat: 36,
        delaySeconds: { minimum: -0.02, maximum: 0.05 },
        speedAngstromPerSecond: { minimum: 2.5, maximum: 8 },
        dragPerSecond: 1.1,
        liftAngstromPerSecondSquared: -4,
        coolingSeconds: { minimum: 0.25, maximum: 0.5 },
        widthPixels: { minimum: 1.2, maximum: 2.8 }
    },
    ember: {
        countAtFullHeat: 12,
        delaySeconds: { minimum: 0, maximum: 0 },
        speedAngstromPerSecond: { minimum: 0, maximum: 0.3 },
        dragPerSecond: 3.2,
        liftAngstromPerSecondSquared: 0.35,
        coolingSeconds: { minimum: 0.6, maximum: 1 },
        widthPixels: { minimum: 1.1, maximum: 1.8 }
    }
};
