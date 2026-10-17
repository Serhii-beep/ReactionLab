import { ConditionsParticipantEntry, ConditionsReactionEntry, StandardState } from "../../../core/chemistry/chemistry-contracts";
import { ReactionParticipant, ReactionSummary } from "../../../data/reactions/reaction";
import { MatterState } from "../../../data/substances/substance";

const STANDARD_STATE_BY_MATTER_STATE: Readonly<Partial<Record<MatterState, StandardState>>> = {
    Solid: 'Solid',
    Liquid: 'Liquid',
    Gas: 'Gas',
    Aqueous: 'Aqueous'
};

export function conditionsEntryOf(
    reaction: ReactionSummary,
    molesOf: (participant: ReactionParticipant) => number
): ConditionsReactionEntry | null {
    const participants: ConditionsParticipantEntry[] = [];

    for (const participant of reaction.participants) {
        const phase = participant.state === null ? undefined : STANDARD_STATE_BY_MATTER_STATE[participant.state];

        if (phase === undefined) {
            return null;
        }

        participants.push({
            formula: participant.formula,
            phase,
            stoichiometricNumber: participant.role === 'Reactant' ? -participant.coefficient : participant.coefficient,
            moles: molesOf(participant)
        });
    }

    return { participants, activationEnergyKilojoulesPerMole: reaction.activationEnergyKilojoulesPerMole ?? undefined };
}

export function stoichiometricMolesOf(participant: ReactionParticipant): number {
    return participant.role === 'Reactant' ? participant.coefficient : 0;
}

export function inWater(reaction: ReactionSummary): boolean {
    return reaction.participants.some((participant) => participant.state === 'Aqueous');
}
