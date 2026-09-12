declare module 'n8ao' {
    import { Camera, Color, Scene } from "three";
    import { Pass } from 'postprocessing';

    export interface N8AOConfiguration {
        aoRadius: number;
        distanceFalloff: number;
        intensity: number;
        color: Color;
        halfRes: boolean;
        screenSpaceRadius: boolean;
        gammaCorrection: boolean;
    }

    export type N8AOQualityMode = 'Performance' | 'Low' | 'Medium' | 'High' | 'Ultra';

    export class N8AOPostPass extends Pass {
        readonly configuration: N8AOConfiguration;
        constructor(scene: Scene, camera: Camera, width: number, height: number);
        setQualityMode(mode: N8AOQualityMode): void;
        setSize(width: number, height: number): void;
        dispose(): void;
    }
}