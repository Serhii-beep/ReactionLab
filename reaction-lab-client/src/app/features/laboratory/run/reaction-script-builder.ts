import { ElementSummary } from "../../../data/elements/element";
import { ReactionTimeline } from "../../../data/reactions/reaction-phases";
import { SubstanceDetail } from "../../../data/substances/substance";
import { ReactionScript } from "../../../engine/animation/reaction-script";
import { WorkspaceItem } from "../../../state/workspace-store";
import { buildBenchUnits } from "../scene/bench-units";

export interface ScriptSources {
    readonly entriesBefore: readonly WorkspaceItem[];
    readonly entriesAfter: readonly WorkspaceItem[];
    readonly details: ReadonlyMap<string, SubstanceDetail>;
    readonly elements: readonly ElementSummary[];
}

export function buildReactionScript(timeline: ReactionTimeline, sources: ScriptSources): ReactionScript {
    return {
        durationSeconds: timeline.durationSeconds,
        cues: {
            gatheredSeconds: timeline.byName.approach.endSeconds,
            swapSeconds: timeline.byName.transitionState.endSeconds,
            releaseSeconds: timeline.byName.separation.startSeconds
        },
        unitsBefore: buildBenchUnits(sources.entriesBefore, sources.details, sources.elements),
        unitsAfter: buildBenchUnits(sources.entriesAfter, sources.details, sources.elements)
    };
}