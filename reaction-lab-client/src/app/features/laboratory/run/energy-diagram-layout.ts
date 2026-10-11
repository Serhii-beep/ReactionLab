import { ReactionTimeline } from "../../../data/reactions/reaction-phases";
import { energyAt, EnergyProfile } from "./energy-profile";

export interface PlotSize {
    readonly widthPixels: number;
    readonly heightPixels: number;
}

export interface PlotOffset {
    readonly left: number;
    readonly top: number;
}

export interface LabelPlacement {
    readonly leftPercent: number;
    readonly topPercent: number;
}

export interface EnergyDiagramPaths {
    readonly axes: string;
    readonly axisHeads: string;
    readonly phaseGuides: string;
    readonly reactantLevel: string;
    readonly catalogCurve: string;
    readonly missingBarrierCurve: string;
    readonly activationArrow: string;
    readonly enthalpyArrow: string;
    readonly breakMarks: string;
}

export interface EnergyDiagramLabels {
    readonly reactants: LabelPlacement;
    readonly reactantsBelowLevel: boolean;
    readonly products: LabelPlacement;
    readonly peak: LabelPlacement;
    readonly enthalpy: LabelPlacement;
    readonly enthalpyBesideArrow: boolean;
    readonly bondsBreak: LabelPlacement;
    readonly bondsForm: LabelPlacement;
}

export interface DiagramProgress {
    readonly marker: PlotOffset;
    readonly markerGuide: string;
    readonly markerGuideShown: boolean;
    readonly traveledCatalogCurve: string;
    readonly traveledMissingBarrierCurve: string;
}

interface EnergyScale {
    readonly breakEnergy: number | null;
    readonly breakTop: number;
    topOf(kilojoulesPerMole: number): number;
}

type ArrowheadDirection = 'up' | 'down' | 'right';

const PLOT_TOP_SHARE = 0.2;
const PLOT_BOTTOM_SHARE = 0.88;
const BREAK_BARRIER_SHARE = 0.08;
const BREAK_BARRIERS_BELOW_ZERO = 1.3;
const BREAK_TOP_SHARE = 0.6;
const BREAK_GAP_PIXELS = 10;
const ENTHALPY_ARROW_INSET_PIXELS = 24;
const SHORTEST_LABELED_ARROW_SHARE = 0.4;
const PLATEAU_LABEL_SHARE = 0.14;
const SAMPLES_PER_RUN = 180;
const CROSSING_ITERATIONS = 24;
const ARROWHEAD = { halfWidth: 4, length: 7, clearance: 2 };
const AXIS_ABOVE_PLOT_PIXELS = 12;
const GUIDE_ABOVE_PLOT_PIXELS = 6;
const LEVEL_PAST_ARROW_PIXELS = 10;
const MARKER_GUIDE_GAP_PIXELS = 6;
const ON_LEVEL_PIXELS = 0.5;

export class EnergyDiagramLayout {
    readonly axisBroken: boolean;
    readonly paths: EnergyDiagramPaths;
    readonly labels: EnergyDiagramLabels;

    private readonly plotTop: number;
    private readonly plotBottom: number;
    private readonly enthalpyArrowLeft: number;
    private readonly scale: EnergyScale;

    constructor(
        private readonly profile: EnergyProfile,
        private readonly timeline: ReactionTimeline,
        private readonly size: PlotSize
    ) {
        this.plotTop = PLOT_TOP_SHARE * size.heightPixels;
        this.plotBottom = PLOT_BOTTOM_SHARE * size.heightPixels;
        this.enthalpyArrowLeft = size.widthPixels - ENTHALPY_ARROW_INSET_PIXELS;
        this.scale = energyScaleOf(profile, this.plotTop, this.plotBottom);
        this.axisBroken = this.scale.breakEnergy !== null;
        this.paths = this.pathsOf();
        this.labels = this.labelsOf();
    }

    progressAt(seconds: number): DiagramProgress {
        const { riseSeconds, settleSeconds, activationKilojoulesPerMole } = this.profile;
        const marker = this.offsetAt(seconds);
        const markerGuide = `M${pointOf({ left: marker.left, top: marker.top + MARKER_GUIDE_GAP_PIXELS })} L${pointOf({ left: marker.left, top: this.plotBottom })}`;
        const offLevels = Math.min(Math.abs(marker.top - this.reactantTop()), Math.abs(marker.top - this.productTop())) > ON_LEVEL_PIXELS;
        const markerGuideShown = offLevels && marker.top + MARKER_GUIDE_GAP_PIXELS < this.plotBottom;

        if (activationKilojoulesPerMole !== null) {
            return { marker, markerGuide, markerGuideShown, traveledCatalogCurve: this.curvePath(0, seconds), traveledMissingBarrierCurve: '' };
        }

        return {
            marker,
            markerGuide,
            markerGuideShown,
            traveledCatalogCurve: this.curvePath(0, Math.min(seconds, riseSeconds)) + this.curvePath(settleSeconds, seconds),
            traveledMissingBarrierCurve: this.curvePath(riseSeconds, Math.min(seconds, settleSeconds))
        };
    }

    private offsetAt(seconds: number): PlotOffset {
        return { left: this.leftOf(seconds), top: this.scale.topOf(energyAt(this.profile, seconds)) };
    }

    private leftOf(seconds: number): number {
        return (seconds / this.timeline.durationSeconds) * this.size.widthPixels;
    }

    private pathsOf(): EnergyDiagramPaths {
        const { riseSeconds, settleSeconds, activationKilojoulesPerMole } = this.profile;
        const durationSeconds = this.timeline.durationSeconds;
        const barrierKnown = activationKilojoulesPerMole !== null;

        return {
            axes: `M0.5 ${(this.plotTop - AXIS_ABOVE_PLOT_PIXELS).toFixed(1)} L0.5 ${(this.plotBottom + 0.5).toFixed(1)} L${this.size.widthPixels} ${(this.plotBottom + 0.5).toFixed(1)}`,
            axisHeads: arrowheadPath({ left: 0.5, top: this.plotTop - AXIS_ABOVE_PLOT_PIXELS - 2 }, 'up') + arrowheadPath({ left: this.size.widthPixels, top: this.plotBottom + 0.5 }, 'right'),
            phaseGuides: this.timeline.phases.slice(1).map((phase) => this.guidePath(this.leftOf(phase.startSeconds))).join(' '),
            reactantLevel: this.reactantLevelPath(),
            catalogCurve: barrierKnown ? this.curvePath(0, durationSeconds) : this.curvePath(0, riseSeconds) + this.curvePath(settleSeconds, durationSeconds),
            missingBarrierCurve: barrierKnown ? '' : this.curvePath(riseSeconds, settleSeconds),
            activationArrow: barrierKnown && activationKilojoulesPerMole > 0 ? this.activationArrowPath(activationKilojoulesPerMole) : '',
            enthalpyArrow: this.profile.enthalpyKilojoulesPerMole === 0 ? '' : this.enthalpyArrowPath(),
            breakMarks: this.breakMarksPath()
        };
    }

    private labelsOf(): EnergyDiagramLabels {
        const { riseSeconds, peakSeconds, settleSeconds, activationKilojoulesPerMole, enthalpyKilojoulesPerMole } = this.profile;
        const { bondsBreak, bondsForm } = this.timeline.byName;
        const reactantTop = this.reactantTop();
        const productTop = this.productTop();
        const peakLabelSeconds = activationKilojoulesPerMole === null ? (riseSeconds + settleSeconds) / 2 : peakSeconds;
        const enthalpyBesideArrow = enthalpyKilojoulesPerMole > 0 || productTop - reactantTop >= SHORTEST_LABELED_ARROW_SHARE * this.size.heightPixels;
        const enthalpyTop = enthalpyBesideArrow ? (reactantTop + productTop) / 2 : Math.min(reactantTop, productTop - PLATEAU_LABEL_SHARE * this.size.heightPixels);

        return {
            reactants: this.placementOf({ left: 0, top: reactantTop }),
            reactantsBelowLevel: reactantTop <= this.plotTop,
            products: this.placementOf({ left: this.leftOf(settleSeconds), top: productTop }),
            peak: this.placementOf({ left: this.leftOf(peakLabelSeconds), top: this.plotTop }),
            enthalpy: this.placementOf({ left: this.enthalpyArrowLeft, top: enthalpyTop }),
            enthalpyBesideArrow,
            bondsBreak: this.placementOf({ left: this.leftOf((bondsBreak.startSeconds + bondsBreak.endSeconds) / 2), top: this.size.heightPixels }),
            bondsForm: this.placementOf({ left: this.leftOf((bondsForm.startSeconds + bondsForm.endSeconds) / 2), top: this.size.heightPixels })
        };
    }

    private placementOf(offset: PlotOffset): LabelPlacement {
        return { leftPercent: (100 * offset.left) / this.size.widthPixels, topPercent: (100 * offset.top) / this.size.heightPixels };
    }

    private reactantTop(): number {
        return this.scale.topOf(0);
    }

    private productTop(): number {
        return this.scale.topOf(this.profile.enthalpyKilojoulesPerMole);
    }

    private reactantLevelPath(): string {
        const top = this.reactantTop();
        const fromRise = pointOf({ left: this.leftOf(this.profile.riseSeconds), top });
        const pastArrow = pointOf({ left: this.enthalpyArrowLeft + LEVEL_PAST_ARROW_PIXELS, top });
        const productDropPixels = this.productTop() - top;
        const productsLabelCrossesLevel = productDropPixels > 0 && productDropPixels < PLATEAU_LABEL_SHARE * this.size.heightPixels;

        if (productsLabelCrossesLevel) {
            const toSettle = pointOf({ left: this.leftOf(this.profile.settleSeconds), top });
            const beforeArrow = pointOf({ left: this.enthalpyArrowLeft - LEVEL_PAST_ARROW_PIXELS, top });

            return `M${fromRise} L${toSettle} M${beforeArrow} L${pastArrow}`;
        }

        return `M${fromRise} L${pastArrow}`;
    }

    private guidePath(left: number): string {
        return `M${pointOf({ left, top: this.plotTop - GUIDE_ABOVE_PLOT_PIXELS })} L${pointOf({ left, top: this.plotBottom })}`;
    }

    private curvePath(fromSeconds: number, toSeconds: number): string {
        if (toSeconds <= fromSeconds) {
            return '';
        }

        const samples = Math.max(Math.ceil(((toSeconds - fromSeconds) / this.timeline.durationSeconds) * SAMPLES_PER_RUN), 2);
        const points: string[] = [];

        for (let sample = 0; sample <= samples; sample++) {
            points.push(pointOf(this.offsetAt(fromSeconds + ((toSeconds - fromSeconds) * sample) / samples)));
        }

        return `M${points.join(' L')}`;
    }

    private activationArrowPath(activationKilojoulesPerMole: number): string {
        const left = this.leftOf(this.profile.peakSeconds);

        return verticalArrowPath({ left, top: this.reactantTop() - 1 }, { left, top: this.scale.topOf(activationKilojoulesPerMole) + ARROWHEAD.clearance });
    }

    private enthalpyArrowPath(): string {
        const left = this.enthalpyArrowLeft;
        const clearance = this.profile.enthalpyKilojoulesPerMole < 0 ? -ARROWHEAD.clearance : ARROWHEAD.clearance;

        return verticalArrowPath({ left, top: this.reactantTop() }, { left, top: this.productTop() + clearance });
    }

    private breakMarksPath(): string {
        const breakEnergy = this.scale.breakEnergy;

        if (breakEnergy === null) {
            return '';
        }

        const gapMiddle = this.scale.breakTop + BREAK_GAP_PIXELS / 2;

        return [1, this.leftOf(this.descentSecondsAt(breakEnergy)), this.enthalpyArrowLeft]
            .map((left) => [-2.5, 2.5].map((shift) => `M${pointOf({ left: left - 5, top: gapMiddle + shift + 3 })} L${pointOf({ left: left + 5, top: gapMiddle + shift - 3 })}`).join(' '))
            .join(' ');
    }

    private descentSecondsAt(kilojoulesPerMole: number): number {
        let earlierSeconds = this.profile.peakSeconds;
        let laterSeconds = this.profile.settleSeconds;

        for (let i = 0; i < CROSSING_ITERATIONS; i++) {
            const middleSeconds = (earlierSeconds + laterSeconds) / 2;

            if (energyAt(this.profile, middleSeconds) > kilojoulesPerMole) {
                earlierSeconds = middleSeconds;
            } else {
                laterSeconds = middleSeconds;
            }
        }

        return (earlierSeconds + laterSeconds) / 2;
    }
}

function energyScaleOf(profile: EnergyProfile, plotTop: number, plotBottom: number): EnergyScale {
    const { enthalpyKilojoulesPerMole: enthalpy, activationKilojoulesPerMole: activation } = profile;
    const highest = Math.max(0, activation ?? 0, enthalpy);
    const lowest = Math.min(0, enthalpy);
    const span = Math.max(highest - lowest, Number.EPSILON);

    if (activation === null || activation <= 0 || enthalpy >= 0 || activation / span >= BREAK_BARRIER_SHARE) {
        return { breakEnergy: null, breakTop: plotBottom, topOf: (kilojoulesPerMole) => plotTop + ((highest - kilojoulesPerMole) / span) * (plotBottom - plotTop) };
    }

    const breakEnergy = -BREAK_BARRIERS_BELOW_ZERO * activation;
    const breakTop = plotTop + BREAK_TOP_SHARE * (plotBottom - plotTop);
    const lowerTop = breakTop + BREAK_GAP_PIXELS;

    return {
        breakEnergy,
        breakTop,
        topOf: (kilojoulesPerMole) => kilojoulesPerMole >= breakEnergy
            ? plotTop + ((highest - kilojoulesPerMole) / (highest - breakEnergy)) * (breakTop - plotTop)
            : lowerTop + ((breakEnergy - kilojoulesPerMole) / (breakEnergy - lowest)) * (plotBottom - lowerTop)
    };
}

function verticalArrowPath(tail: PlotOffset, tip: PlotOffset): string {
    const pointsUp = tip.top < tail.top;
    const headBaseTop = tip.top + (pointsUp ? ARROWHEAD.length : -ARROWHEAD.length);
    const hasShaft = pointsUp ? tail.top > headBaseTop : tail.top < headBaseTop;
    const shaft = hasShaft ? `M${pointOf(tail)} L${pointOf({ left: tip.left, top: headBaseTop })} ` : '';

    return shaft + arrowheadPath(tip, pointsUp ? 'up' : 'down');
}

function arrowheadPath(tip: PlotOffset, direction: ArrowheadDirection): string {
    const { halfWidth, length } = ARROWHEAD;

    if (direction === 'right') {
        return `M${pointOf({ left: tip.left - length, top: tip.top - halfWidth })} L${pointOf(tip)} L${pointOf({ left: tip.left - length, top: tip.top + halfWidth })} Z`;
    }

    const baseTop = tip.top + (direction === 'up' ? length : -length);

    return `M${pointOf({ left: tip.left - halfWidth, top: baseTop })} L${pointOf(tip)} L${pointOf({ left: tip.left + halfWidth, top: baseTop })} Z`;
}

function pointOf(offset: PlotOffset): string {
    return `${offset.left.toFixed(1)} ${offset.top.toFixed(1)}`;
}