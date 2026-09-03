export type SubstanceKind = 'Molecular' | 'Ionic' | 'Metallic' | 'Monatomic' | 'NetworkCovalent';

export type MatterState = 'Solid' | 'Liquid' | 'Gas' | 'Aqueous' | 'Plasma';

export interface SubstanceSummary {
    readonly id: string;
    readonly formula: string;
    readonly name: string;
    readonly kind: SubstanceKind;
    readonly isOrganic: boolean;
    readonly stateAtRoomTemperature: MatterState;
    readonly weightGramsPerMole: number | null;
    readonly category: string | null;
}