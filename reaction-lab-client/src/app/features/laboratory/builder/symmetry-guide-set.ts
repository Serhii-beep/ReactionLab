import { BenchLayout } from "../../../engine/scene/bench-layout";
import { SymmetryGuideSet } from "../../../engine/objects/symmetry-guides";
import { PlacedSketch } from "./sketch-geometry";

const GUIDE_PADDING = 0.6;

export function symmetryGuidesFor(placed: PlacedSketch, layout: BenchLayout): SymmetryGuideSet | null {
    const group = placed.pointGroup;
    const anchor = layout.atoms[0];

    if (group === null || anchor === undefined || placed.positions.length === 0) {
        return null;
    }

    const offset = anchor.position.clone().sub(placed.positions[0]);
    const radius = placed.positions.reduce((farthest, position) => Math.max(farthest, position.distanceTo(group.center)), 0);

    return {
        center: group.center.clone().add(offset),
        radius: radius + GUIDE_PADDING,
        axes: group.axes,
        planeNormals: group.planeNormals,
        inversionCenter: group.inversionCenter
    };
}
