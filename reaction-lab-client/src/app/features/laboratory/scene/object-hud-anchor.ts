import { LayoutUnit } from "../../../engine/scene/bench-layout";

export function resolveHudUnitId (
    selectedSubstanceId: string | null,
    clickedUnitId: string | null,
    hoveredUnitId: string | null,
    units: readonly LayoutUnit[]
): string | null {
    if (selectedSubstanceId === null) {
        return hoveredUnitId;
    }

    if (clickedUnitId !== null && units.some((unit) => unit.id === clickedUnitId && unit.substanceId === selectedSubstanceId)) {
        return clickedUnitId;
    }

    return units.find((unit) => unit.substanceId === selectedSubstanceId)?.id ?? null;
}