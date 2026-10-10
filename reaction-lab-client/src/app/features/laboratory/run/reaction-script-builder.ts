import { ElementSummary } from "../../../data/elements/element";
import { ReactionSummary } from "../../../data/reactions/reaction";
import { ReactionPhase, ReactionTimeline } from "../../../data/reactions/reaction-phases";
import { SubstanceDetail } from "../../../data/substances/substance";
import { PhaseSpansByName, PhaseSpanSeconds, ReactionScript } from "../../../engine/animation/reaction-script";
import { EmissionPlan } from "../../../engine/particles/emission-plan";
import { WorkspaceItem } from "../../../state/workspace-store";
import { buildBenchUnits } from "../scene/bench-units";
import { reactionEnergeticsOf } from "./reaction-energetics";
import { gasLookOf } from "./gas-looks";
import { precipitatePlanOf } from "./precipitates";

export interface ScriptSources {
    readonly entriesBefore: readonly WorkspaceItem[];
    readonly entriesAfter: readonly WorkspaceItem[];
    readonly details: ReadonlyMap<string, SubstanceDetail>;
    readonly elements: readonly ElementSummary[];
}

export function buildReactionScript(
    reaction: ReactionSummary,
    timeline: ReactionTimeline,
    emissions: readonly EmissionPlan[],
    sources: ScriptSources
): ReactionScript {
    const { approach, collision, bondsBreak, transitionState, bondsForm, separation } = timeline.byName;
    const elements = new Map(sources.elements.map((element) => [element.symbol, element]));
    const precipitate = precipitatePlanOf(reaction, { entriesAfter: sources.entriesAfter, details: sources.details, elements });
    const phases: PhaseSpansByName = {
        approach: spanOf(approach),
        collision: spanOf(collision),
        bondsBreak: spanOf(bondsBreak),
        transitionState: spanOf(transitionState),
        bondsForm: spanOf(bondsForm),
        separation: spanOf(separation)
    };

    return {
        durationSeconds: timeline.durationSeconds,
        phases,
        energetics: reactionEnergeticsOf(reaction),
        massBySymbol: new Map(sources.elements.map((element) => [element.symbol, element.mass])),
        randomSeed: reaction.id,
        emissions,
        gasLook: gasLookOf(reaction) ?? (precipitate === null ? null : 'precipitate'),
        precipitate,
        unitsBefore: buildBenchUnits(sources.entriesBefore, sources.details, sources.elements),
        unitsAfter: buildBenchUnits(sources.entriesAfter, sources.details, sources.elements)
    };
}

function spanOf(phase: ReactionPhase): PhaseSpanSeconds {
    return { start: phase.startSeconds, end: phase.endSeconds };
}
