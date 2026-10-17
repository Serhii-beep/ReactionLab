import { HighlightLevelsByUnitId } from "../../../engine/objects/selection-outline";

export function sameHighlightLevels(first: HighlightLevelsByUnitId, second: HighlightLevelsByUnitId): boolean {
    if (first.size !== second.size) {
        return false;
    }

    for (const [id, level] of first) {
        if (second.get(id) !== level) {
            return false;
        }
    }

    return true;
}
