import { Vector3 } from "three";

export const SPARK_SIDE = 64;
export const SPARK_SLOTS = SPARK_SIDE * SPARK_SIDE;
export const LIFE_PER_COOLING = 4;

export interface SparkLaunch {
    readonly seconds: number;
    readonly place: Vector3;
    readonly velocity: Vector3;
    readonly kelvin: number;
    readonly dragPerSecond: number;
    readonly liftAngstromPerSecondSquared: number;
    readonly coolingSeconds: number;
    readonly widthPixels: number;
}
