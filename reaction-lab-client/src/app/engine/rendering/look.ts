export type LookName = 'light' | 'dark';

export interface LightLook {
    intensity: number;
    color: number;
}

export interface Look {
    exposure: number;
    environmentIntensity: number;
    key: LightLook;
    rim: LightLook;
    fill: { intensity: number; sky: string; ground: string };
    shadowIntensity: number;
    ground: string;
    fog: { token: string; near: number; far: number; }
}

export const LOOKS: Readonly<Record<LookName, Look>> = {
    light: {
        exposure: 0.95,
        environmentIntensity: 0.7,
        key: { intensity: 2.4, color: 0xfff1e0 },
        rim: { intensity: 1.0, color: 0xffffff },
        fill: { intensity: 0.8, sky: '--surface-raised', ground: '--surface-void' },
        shadowIntensity: 0.7,
        ground: '--surface-raised',
        fog: { token: '--surface-void', near: 20, far: 46 }
    },
    dark: {
        exposure: 1.0,
        environmentIntensity: 0.55,
        key: { intensity: 2.2, color: 0xfff1e0 },
        rim: { intensity: 1.4, color: 0xcfe6ff },
        fill: { intensity: 0.5, sky: '--surface-active', ground: '--surface-void' },
        shadowIntensity: 0.8,
        ground: '--surface-base',
        fog: { token: '--surface-void', near: 20, far: 46 }
    }
};