import { Sphere } from "three";
import { PlacedAtom, PlacedBond } from "../scene/bench-layout";
import { AtomPair } from "./atom-pairing";
import { DynamicsSchedule } from "./dynamics/dynamics-schedule";
import { Gathering } from "./gathering";
import { ReactionScript } from "./reaction-script";
import { ApproachArc, approachArcOf } from "./dynamics/approach-guides";
import { IndexedBond } from "./dynamics/bonding-picture";
import { flattened, SimulatedAtom } from "./dynamics/molecular-system";
import { DynamicsInput } from "./dynamics/reaction-dynamics";

export type RecordingIndexByAtom = ReadonlyMap<PlacedAtom, number>;

export interface DynamicsSources {
    readonly script: ReactionScript;
    readonly schedule: DynamicsSchedule;
    readonly atomPairs: readonly AtomPair[];
    readonly reactantIndexByAtom: RecordingIndexByAtom;
    readonly productIndexByAtom: RecordingIndexByAtom;
    readonly reactantBonds: readonly PlacedBond[];
    readonly productBonds: readonly PlacedBond[];
    readonly gathering: Gathering;
    readonly restSphereByUnitId: ReadonlyMap<string, Sphere>;
}

const UNKNOWN_ELEMENT_MASS = 12;

export function buildDynamicsInput(sources: DynamicsSources): DynamicsInput {
    const { script, atomPairs, gathering } = sources;
    const reactantAtoms = atomPairs.map((pair) => pair.reactant);
    const productAtoms = atomPairs.map((pair) => pair.product);

    return {
        atoms: reactantAtoms.map((atom) => simulatedAtomOf(atom, script.massBySymbol)),
        reactantRest: flattened(reactantAtoms.map((atom) => atom.position)),
        productRest: flattened(productAtoms.map((atom) => atom.position)),
        reactantBonds: indexedBondsOf(sources.reactantBonds, sources.reactantIndexByAtom),
        productBonds: indexedBondsOf(sources.productBonds, sources.productIndexByAtom),
        reactantUnitIds: reactantAtoms.map((atom) => atom.unitId),
        productUnitIds: productAtoms.map((atom) => atom.unitId),
        approachArcByUnitId: approachArcsOf(gathering, sources.restSphereByUnitId),
        schedule: sources.schedule,
        energetics: script.energetics,
        randomSeed: script.randomSeed
    };
}

export function recordingIndexByAtom(atoms: readonly PlacedAtom[]): RecordingIndexByAtom {
    return new Map(atoms.map((atom, index) => [atom, index]));
}

function simulatedAtomOf(atom: PlacedAtom, massBySymbol: ReadonlyMap<string, number>): SimulatedAtom {
    return { symbol: atom.symbol, mass: massBySymbol.get(atom.symbol) ?? UNKNOWN_ELEMENT_MASS, radius: atom.radius };
}

function indexedBondsOf(bonds: readonly PlacedBond[], indexByAtom: RecordingIndexByAtom): IndexedBond[] {
    return bonds.flatMap((bond) => {
        const first = indexByAtom.get(bond.from);
        const second = indexByAtom.get(bond.to);

        return first === undefined || second === undefined ? [] : [{ first, second, kind: bond.kind }];
    });
}

function approachArcsOf(gathering: Gathering, restSphereByUnitId: ReadonlyMap<string, Sphere>): Map<string, ApproachArc> {
    const arcByUnitId = new Map<string, ApproachArc>();

    for (const [unitId, slot] of gathering.centerByUnitId) {
        const sphere = restSphereByUnitId.get(unitId);

        if (sphere) {
            arcByUnitId.set(unitId, approachArcOf(sphere.center, slot, gathering.meeting));
        }
    }

    return arcByUnitId;
}