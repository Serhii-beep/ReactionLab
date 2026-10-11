import { ElementSummary } from "../../../data/elements/element";
import { ReactionSummary } from "../../../data/reactions/reaction";
import { ReactionPhase, ReactionTimeline } from "../../../data/reactions/reaction-phases";
import { SubstanceDetail } from "../../../data/substances/substance";
import { PhaseSpansByName, PhaseSpanSeconds, ReactionScript } from "../../../engine/animation/reaction-script";
import { WorkspaceItem } from "../../../state/workspace-store";
import { buildBenchUnits } from "../scene/bench-units";
import { reactionEnergeticsOf } from "./reaction-energetics";
import { gasPlanOf } from "./gas-plans";
import { precipitatePlanOf } from "./precipitates";

export interface ScriptSources {
    readonly entriesBefore: readonly WorkspaceItem[];
    readonly entriesAfter: readonly WorkspaceItem[];
    readonly details: ReadonlyMap<string, SubstanceDetail>;
    readonly elements: readonly ElementSummary[];
}

export function buildReactionScript(reaction: ReactionSummary, timeline: ReactionTimeline, sources: ScriptSources): ReactionScript {
    const elements = new Map(sources.elements.map((element) => [element.symbol, element]));
    const precipitate = precipitatePlanOf(reaction, { entriesAfter: sources.entriesAfter, details: sources.details, elements });

    return {
        durationSeconds: timeline.durationSeconds,
        phases: phaseSpansOf(timeline),
        energetics: reactionEnergeticsOf(reaction),
        massBySymbol: new Map(sources.elements.map((element) => [element.symbol, element.mass])),
        randomSeed: reaction.id,
        gas: gasPlanOf(reaction, sources.details, precipitate),
        precipitate,
        unitsBefore: buildBenchUnits(sources.entriesBefore, sources.details, sources.elements),
        unitsAfter: buildBenchUnits(sources.entriesAfter, sources.details, sources.elements)
    };
}

export function phaseSpansOf(timeline: ReactionTimeline): PhaseSpansByName {
    const { approach, collision, bondsBreak, transitionState, bondsForm, separation } = timeline.byName;

    return {
        approach: spanOf(approach),
        collision: spanOf(collision),
        bondsBreak: spanOf(bondsBreak),
        transitionState: spanOf(transitionState),
        bondsForm: spanOf(bondsForm),
        separation: spanOf(separation)
    };
}

function spanOf(phase: ReactionPhase): PhaseSpanSeconds {
    return { start: phase.startSeconds, end: phase.endSeconds };
}