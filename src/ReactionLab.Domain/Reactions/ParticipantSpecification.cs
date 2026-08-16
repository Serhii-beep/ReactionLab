using ReactionLab.Domain.Enums;
using ReactionLab.Domain.Substances;

namespace ReactionLab.Domain.Reactions;

public sealed record ParticipantSpecification(
    SubstanceId SubstanceId,
    ChemicalFormula Formula,
    ParticipantRole Role,
    int Coefficient,
    MatterState? State);
