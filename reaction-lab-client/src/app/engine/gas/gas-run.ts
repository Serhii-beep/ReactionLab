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
    readonly cloudPerSecond: number;
}

export interface GasObstacle {
    readonly place: Vector3;
    readonly velocity: Vector3;
    readonly radius: number;
}

export interface GasCloud {
    readonly atomIndices: readonly number[];
    readonly albedo: Color;
}

export interface GasRun {
    readonly look: GasLookName;
    readonly trace: ReactionTrace;
    readonly waterOxygens: readonly number[];
    readonly releasesHeat: boolean;
    readonly cloud: GasCloud | null;
}
