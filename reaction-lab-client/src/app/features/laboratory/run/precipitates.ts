import { ReactionSummary } from "../../../data/reactions/reaction";
import { SubstanceDetail } from "../../../data/substances/substance";
import { LayoutUnit } from "../../../engine/scene/bench-layout";
import { PrecipitatePlan } from "../../../engine/animation/reaction-script";
import { WorkspaceItem } from "../../../state/workspace-store";
import { ElementsBySymbol } from "../scene/atom-balls";
import { MAX_COPIES } from "../scene/bench-units";
import { ionicFormulaOf, latticeOf, unitOfIons } from "../scene/ionic-units";
import { IonicFormula } from "../scene/ions/ionic-formula";

export interface PrecipitateSources {
    readonly entriesAfter: readonly WorkspaceItem[];
    readonly details: ReadonlyMap<string, SubstanceDetail>;
    readonly elements: ElementsBySymbol;
}

const CRYSTALLITE_IONS = 16;

export function precipitatePlanOf(reaction: ReactionSummary, sources: PrecipitateSources): PrecipitatePlan | null {
    if (!reaction.participants.some((participant) => participant.role === 'Reactant' && participant.state === 'Aqueous')) {
        return null;
    }

    for (const product of reaction.participants.filter((participant) => participant.role === 'Product' && participant.state === 'Solid')) {
        const detail = sources.details.get(product.substanceId);
        const formula = detail === undefined ? null : ionicFormulaOf(detail);

        if (detail !== undefined && formula !== null) {
            return planOf(detail, formula, sources);
        }
    }

    return null;
}

function planOf(detail: SubstanceDetail, formula: IonicFormula, sources: PrecipitateSources): PrecipitatePlan {
    const benchCount = Math.min(sources.entriesAfter.find((entry) => entry.substance.id === detail.id)?.count ?? 0, MAX_COPIES);
    const ionsPerUnit = [...formula.cations, ...formula.anions].reduce((sum, ion) => sum + ion.count, 0);
    const unitCount = Math.max(benchCount, Math.ceil(CRYSTALLITE_IONS / ionsPerUnit));
    const standIns: LayoutUnit[] = latticeOf(detail.formula, formula, unitCount).slice(benchCount).map((ions, index) => ({
        id: `${detail.id}#solution${index}`,
        substanceId: detail.id,
        ...unitOfIons(ions, 'solid', sources.elements),
        crystallite: true
    }));

    return { substanceId: detail.id, standIns };
}
