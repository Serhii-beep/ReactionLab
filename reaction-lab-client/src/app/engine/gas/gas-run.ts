import { Color, Vector3 } from "three";
import { GasLookName } from "./gas-look";
import { ReactionTrace } from "../animation/reaction-trace";

export interface GasSource {
    readonly place: Vector3;
    readonly radius: number;
    readonly targetKelvin: number;
    readonly heatingPerSecond: number;
    readonly vaporPerSecond: number;
    readonly flamePerSecond: number;
    readonly particlesPerSecond: number;
}

export interface GasObstacle {
    readonly place: Vector3;
    readonly velocity: Vector3;
    readonly radius: number;
}

export interface GasParticles {
    readonly atomIndices: readonly number[];
    readonly albedo: Color;
    readonly loading: number;
}

export interface GasRun {
    readonly look: GasLookName;
    readonly trace: ReactionTrace;
    readonly waterOxygens: readonly number[];
    readonly releasesHeat: boolean;
    readonly particles: GasParticles | null;
}
