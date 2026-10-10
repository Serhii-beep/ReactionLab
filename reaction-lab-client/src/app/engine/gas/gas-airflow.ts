import { Texture } from "three";
import { GasBoxUniforms, GasGridUniforms } from "./gas-passes";

export interface GasAirflow {
    readonly simulatedSeconds: number;
    readonly velocity: Texture;
    readonly scalars: Texture;
    readonly grid: GasGridUniforms;
    readonly box: GasBoxUniforms;
}
