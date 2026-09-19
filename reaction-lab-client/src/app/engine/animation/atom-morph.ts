import { Vector3 } from "three";
import { AtomPair } from "./atom-pairing";
import { SurvivingBond } from "./bond-continuity";
import { posedCentroidOf, PosedPoints, posedPositionOf } from "./unit-gathering";

interface AtomPath extends AtomPair {
    readonly gatheredAsReactant: Vector3;
    readonly gatheredAsProduct: Vector3;
}

interface BondCentroidPath extends SurvivingBond {
    readonly gatheredAsReactant: Vector3;
    readonly gatheredAsProduct: Vector3;
}

export class AtomMorph {
    private readonly atomPaths: readonly AtomPath[];
    private readonly bondCentroidPaths: readonly BondCentroidPath[];

    constructor(atomPairs: readonly AtomPair[], survivingBonds: readonly SurvivingBond[], gathered: PosedPoints) {
        this.atomPaths = atomPairs.map((pair) => ({
            ...pair,
            gatheredAsReactant: posedPositionOf(gathered, pair.reactant).clone(),
            gatheredAsProduct: posedPositionOf(gathered, pair.product).clone()
        }));
        this.bondCentroidPaths = survivingBonds.map((bond) => ({
            ...bond,
            gatheredAsReactant: posedCentroidOf(gathered, bond.reactant).clone(),
            gatheredAsProduct: posedCentroidOf(gathered, bond.product).clone()
        }));
    }

    placeReactants(progress: number): void {
        for (const path of this.atomPaths) {
            path.reactant.position.lerpVectors(path.gatheredAsReactant, path.gatheredAsProduct, progress);
        }

        for (const path of this.bondCentroidPaths) {
            path.reactant.centroid.lerpVectors(path.gatheredAsReactant, path.gatheredAsProduct, progress);
        }
    }

    placeProducts(progress: number): void {
        for (const path of this.atomPaths) {
            path.product.position.lerpVectors(path.gatheredAsReactant, path.gatheredAsProduct, progress);
        }

        for (const path of this.bondCentroidPaths) {
            path.product.centroid.lerpVectors(path.gatheredAsReactant, path.gatheredAsProduct, progress);
        }
    }
}