import { PlacedBond } from "../scene/bench-layout";
import { BondChanges } from "./bond-continuity";
import { DynamicsSources, RecordingIndexByAtom } from "./dynamics-input-builder";
import { DynamicsRecording, recordedPositionAt } from "./dynamics/dynamics-recording";
import { EnergyLedger } from "./energy-ledger";
import { smoothProgressBetween } from "./easing";
import { PhaseSpanSeconds } from "./reaction-script";
import { ReactionTrace, RecordedBond } from "./reaction-trace";
import { radiusBlendOf } from "./radius-blend";

export function buildReactionTrace(sources: DynamicsSources, bondChanges: BondChanges, ledger: EnergyLedger, recording: DynamicsRecording, landing: PhaseSpanSeconds): ReactionTrace {
    const { script, schedule, atomPairs } = sources;
    const restPlaces = atomPairs.map((pair) => pair.product.position.clone());

    return {
        schedule,
        randomSeed: script.randomSeed,
        atomCount: atomPairs.length,
        radiusAt: radiusBlendOf(atomPairs, schedule),
        reactantUnitIds: atomPairs.map((pair) => pair.reactant.unitId),
        breakingBonds: recordedBondsOf(bondChanges.breaking, sources.reactantIndexByAtom),
        formingBonds: recordedBondsOf(bondChanges.forming, sources.productIndexByAtom),
        meeting: sources.gathering.meeting.clone(),
        temperatureKelvinAt: (atomIndex, seconds) => ledger.temperatureKelvinAt(atomIndex, seconds),
        placeAt: (atomIndex, seconds, target) => recordedPositionAt(recording, atomIndex, seconds, target)
            .lerp(restPlaces[atomIndex], smoothProgressBetween(landing.start, landing.end, seconds))
    };
}

function recordedBondsOf(bonds: readonly PlacedBond[], indexByAtom: RecordingIndexByAtom): RecordedBond[] {
    return bonds.flatMap(({ from, to }) => {
        const firstAtomIndex = indexByAtom.get(from);
        const secondAtomIndex = indexByAtom.get(to);

        return firstAtomIndex === undefined || secondAtomIndex === undefined ? [] : [{ firstAtomIndex, secondAtomIndex }];
    });
}
