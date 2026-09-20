import { Vector3 } from "three";
import { AtomPair } from "./atom-pairing";
import { SurvivingBond } from "./bond-continuity";
import { posedCentroidOf, PosedPoints, posedPositionOf } from "./unit-gathering";
import { ChoreographyTuning } from "./reaction-script";
import { PlacedAtom, PlacedBond } from "../scene/bench-layout";

export interface MorphContext {
    readonly gathered: PosedPoints;
    readonly meeting: Vector3;
    readonly tuning: ChoreographyTuning;
}

interface AtomPath extends AtomPair {
    readonly gatheredAsReactant: Vector3;
    readonly gatheredAsProduct: Vector3;
    readonly bulgeDirection: Vector3;
    readonly jitterPhases: Vector3;
}

interface BondCentroidPath extends SurvivingBond {
    readonly gatheredAsReactant: Vector3;
    readonly gatheredAsProduct: Vector3;
    readonly fromAtomPath: AtomPath;
    readonly toAtomPath: AtomPath;
}

interface MorphSide {
    readonly atomOf: (path: AtomPath) => PlacedAtom;
    readonly bondOf: (path: BondCentroidPath) => PlacedBond;
}

type AtomPathByReactant = ReadonlyMap<PlacedAtom, AtomPath>;

const REACTANT_SIDE: MorphSide = { atomOf: (path) => path.reactant, bondOf: (path) => path.reactant };
const PRODUCT_SIDE: MorphSide = { atomOf: (path) => path.product, bondOf: (path) => path.product };
const BULGE_UPWARD_BIAS = 0.35;
const PARABOLA_PEAK_NORMALIZER = 4;
const GOLDEN_ANGLE = 2.399963;
const JITTER_PHASE_SEEDS = new Vector3(0, 1, 2);
const JITTER_PHASE_STRIDES = new Vector3(1, 2, 3);

export class AtomMorph {
    private readonly atomPaths: readonly AtomPath[];
    private readonly bondCentroidPaths: readonly BondCentroidPath[];
    private readonly tuning: ChoreographyTuning;
    private readonly displacementScratch = new Vector3();

    constructor(atomPairs: readonly AtomPair[], survivingBonds: readonly SurvivingBond[], context: MorphContext) {
        this.tuning = context.tuning;
        this.atomPaths = atomPairs.map((pair, index) => atomPathOf(pair, index, context));

        const atomPathByReactant: AtomPathByReactant = new Map(this.atomPaths.map((path) => [path.reactant, path]));

        this.bondCentroidPaths = survivingBonds.flatMap((bond) => bondCentroidPathOf(bond, atomPathByReactant, context.gathered));
    }

    placeReactants(rearrangement: number, jitterEnvelope: number, seconds: number): void {
        this.placeSide(REACTANT_SIDE, rearrangement, jitterEnvelope, seconds);
    }

    placeProducts(rearrangement: number, jitterEnvelope: number, seconds: number): void {
        this.placeSide(PRODUCT_SIDE, rearrangement, jitterEnvelope, seconds);
    }

    private placeSide(side: MorphSide, rearrangement: number, jitterEnvelope: number, seconds: number): void {
        for (const path of this.atomPaths) {
            side.atomOf(path).position
                .lerpVectors(path.gatheredAsReactant, path.gatheredAsProduct, rearrangement)
                .add(this.displacementOf(path, rearrangement, jitterEnvelope, seconds));
        }

        for (const path of this.bondCentroidPaths) {
            side.bondOf(path).centroid
                .lerpVectors(path.gatheredAsReactant, path.gatheredAsProduct, rearrangement)
                .addScaledVector(this.displacementOf(path.fromAtomPath, rearrangement, jitterEnvelope, seconds), 0.5)
                .addScaledVector(this.displacementOf(path.toAtomPath, rearrangement, jitterEnvelope, seconds), 0.5);
        }
    }

    private displacementOf(path: AtomPath, rearrangement: number, jitterEnvelope: number, seconds: number): Vector3 {
        const bulge = this.tuning.bulgeAngstrom * PARABOLA_PEAK_NORMALIZER * rearrangement * (1 - rearrangement);
        const jitter = this.tuning.jitterAngstrom * jitterEnvelope;
        const waveRadians = 2 * Math.PI * this.tuning.jitterHertz * seconds;

        this.displacementScratch.copy(path.bulgeDirection).multiplyScalar(bulge);
        this.displacementScratch.x += jitter * Math.sin(waveRadians + path.jitterPhases.x);
        this.displacementScratch.y += jitter * Math.sin(waveRadians + path.jitterPhases.y);
        this.displacementScratch.z += jitter * Math.sin(waveRadians + path.jitterPhases.z);

        return this.displacementScratch;
    }
}

function atomPathOf(pair: AtomPair, index: number, context: MorphContext): AtomPath {
    const gatheredAsReactant = posedPositionOf(context.gathered, pair.reactant).clone();
    const gatheredAsProduct = posedPositionOf(context.gathered, pair.product).clone();
    const bulgeDirection = gatheredAsReactant.clone().add(gatheredAsProduct).multiplyScalar(0.5).sub(context.meeting);

    bulgeDirection.y += BULGE_UPWARD_BIAS;
    bulgeDirection.normalize();

    return {
        ...pair,
        gatheredAsReactant,
        gatheredAsProduct,
        bulgeDirection,
        jitterPhases: new Vector3(
            JITTER_PHASE_SEEDS.x + index * GOLDEN_ANGLE * JITTER_PHASE_STRIDES.x,
            JITTER_PHASE_SEEDS.y + index * GOLDEN_ANGLE * JITTER_PHASE_STRIDES.y,
            JITTER_PHASE_SEEDS.z + index * GOLDEN_ANGLE * JITTER_PHASE_STRIDES.z
        )
    };
}

function bondCentroidPathOf(bond: SurvivingBond, atomPathByReactant: AtomPathByReactant, gathered: PosedPoints): BondCentroidPath[] {
    const fromAtomPath = atomPathByReactant.get(bond.reactant.from);
    const toAtomPath = atomPathByReactant.get(bond.reactant.to);

    if (fromAtomPath === undefined || toAtomPath === undefined) {
        return [];
    }

    return [{
        ...bond,
        gatheredAsReactant: posedCentroidOf(gathered, bond.reactant).clone(),
        gatheredAsProduct: posedCentroidOf(gathered, bond.product).clone(),
        fromAtomPath,
        toAtomPath
    }];
}