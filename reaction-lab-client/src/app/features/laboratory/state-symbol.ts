import { MatterState } from "../../data/substances/substance";
import { ChemicalState } from "../../design-system/chemistry/chem-formula";

const SYMBOLS: Record<MatterState, ChemicalState | undefined> = {
    Solid: 's',
    Liquid: 'l',
    Gas: 'g',
    Aqueous: 'aq',
    Plasma: undefined
};

export function stateSymbol(state: MatterState | null): ChemicalState | undefined {
    return state === null ? undefined : SYMBOLS[state];
}