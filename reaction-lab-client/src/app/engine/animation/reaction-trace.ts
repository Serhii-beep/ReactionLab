import { Vector3 } from "three";
import { DynamicsSchedule } from "./dynamics/dynamics-schedule";
import { RadiusAt } from "./radius-blend";

export interface RecordedBond {
    readonly firstAtomIndex: number;
    readonly secondAtomIndex: number;
}

export interface ReactionTrace {
    readonly schedule: DynamicsSchedule;
    readonly randomSeed: string;
    readonly atomCount: number;
    readonly radiusAt: RadiusAt;
    readonly reactantUnitIds: readonly string[];
    readonly breakingBonds: readonly RecordedBond[];
    readonly formingBonds: readonly RecordedBond[];
    readonly meeting: Vector3;
    readonly temperatureKelvinAt: (atomIndex: number, seconds: number) => number;
    readonly placeAt: (atomIndex: number, seconds: number, target: Vector3) => Vector3;
}
