using ReactionLab.Catalog.Domain.Enums;
using ReactionLab.Catalog.Domain.Substances;

namespace ReactionLab.Catalog.Domain.Reactions;

public sealed record ParticipantSpecification(
    SubstanceId SubstanceId,
    ChemicalFormula Formula,
    ParticipantRole Role,
    int Coefficient,
    MatterState? State);
