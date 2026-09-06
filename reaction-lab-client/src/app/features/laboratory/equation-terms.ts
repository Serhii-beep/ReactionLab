import { ParticipantRole, ReactionSummary } from "../../data/reactions/reaction";
import { EquationTerm } from "../../design-system/chemistry/chem-equation";
import { stateSymbol } from "./state-symbol";

export function equationTerms(reaction: ReactionSummary, role: ParticipantRole): readonly EquationTerm[] {
    return reaction.participants
        .filter((participant) => participant.role === role)
        .map((participant) => ({
            formula: participant.formula,
            coefficient: participant.coefficient,
            state: participant.state === null ? undefined : stateSymbol(participant.state)
        }));
}