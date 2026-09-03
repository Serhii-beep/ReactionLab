import { SubstanceSummary } from "../../data/substances/substance";

export interface SubstanceDrag {
    readonly drag: 'substance';
    readonly substance: SubstanceSummary
}

export function substanceDrag(substance: SubstanceSummary): SubstanceDrag {
    return { drag: 'substance', substance };
}

export function draggedSubstance(payload: unknown): SubstanceSummary | null {
    return isSubstanceDrag(payload) ? payload.substance : null;
}

function isSubstanceDrag(payload: unknown): payload is SubstanceDrag {
    return typeof payload === 'object' && payload !== null && 'drag' in payload && payload.drag === 'substance';
}