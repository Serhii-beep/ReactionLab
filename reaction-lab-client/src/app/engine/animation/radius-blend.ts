import { AtomPair } from "./atom-pairing";
import { DynamicsSchedule, productWeightAt } from "./dynamics/dynamics-schedule";

export type RadiusAt = (atomIndex: number, seconds: number) => number;

export function radiusBlendOf(atomPairs: readonly AtomPair[], schedule: DynamicsSchedule): RadiusAt {
    const reactantRadii = atomPairs.map((pair) => pair.reactant.radius);
    const productRadii = atomPairs.map((pair) => pair.product.radius);

    return (atomIndex, seconds) => reactantRadii[atomIndex] + (productRadii[atomIndex] - reactantRadii[atomIndex]) * productWeightAt(schedule, seconds);
}
