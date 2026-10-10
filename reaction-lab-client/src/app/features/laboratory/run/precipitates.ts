import { Color } from "three";
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
const WHITE_PRECIPITATE = '#f3f4f2';
const PRECIPITATE_COLOR_BY_FORMULA: ReadonlyMap<string, string> = new Map([
    ['PbI2', '#f2c12e'],
    ['AgI', '#eee2a0'],
    ['Ag3PO4', '#ead04a'],
    ['Ag2CO3', '#ede6b4'],
    ['Cu(OH)2', '#6ea6db'],
    ['Cu3(PO4)2', '#62ada6'],
    ['CuCO3', '#4f9a6c'],
    ['Fe(OH)3', '#9c4f2b'],
    ['Fe(OH)2', '#97b487'],
    ['FePO4', '#e9ddb0'],
    ['Ag2O', '#4a3b2f'],
    ['Ag2S', '#2a2a2b'],
    ['PbS', '#2b2b2e'],
    ['CuS', '#26292d'],
    ['FeS', '#2e2b27'],
    ['CuO', '#232323']
]);

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

    return { substanceId: detail.id, cloudColor: new Color(PRECIPITATE_COLOR_BY_FORMULA.get(detail.formula) ?? WHITE_PRECIPITATE), standIns };
}
