import { ElementSummary } from "../../../data/elements/element";
import { ReactionPhase, ReactionTimeline } from "../../../data/reactions/reaction-phases";
import { SubstanceDetail } from "../../../data/substances/substance";
import { PhaseSpansByName, PhaseSpanSeconds, ReactionScript } from "../../../engine/animation/reaction-script";
import { WorkspaceItem } from "../../../state/workspace-store";
import { buildBenchUnits } from "../scene/bench-units";

export interface ScriptSources {
    readonly entriesBefore: readonly WorkspaceItem[];
    readonly entriesAfter: readonly WorkspaceItem[];
    readonly details: ReadonlyMap<string, SubstanceDetail>;
    readonly elements: readonly ElementSummary[];
}

export function buildReactionScript(timeline: ReactionTimeline, sources: ScriptSources): ReactionScript {
    const { approach, collision, bondsBreak, transitionState, bondsForm, separation } = timeline.byName;
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
        unitsBefore: buildBenchUnits(sources.entriesBefore, sources.details, sources.elements),
        unitsAfter: buildBenchUnits(sources.entriesAfter, sources.details, sources.elements)
    };
}

function spanOf(phase: ReactionPhase): PhaseSpanSeconds {
    return { start: phase.startSeconds, end: phase.endSeconds };
}