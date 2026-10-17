using ReactionLab.Catalog.Domain.Enums;

namespace ReactionLab.Catalog.Application.Features.Reactions.Contracts;

public sealed record ReactionParticipantResponse(
    Guid SubstanceId,
    string Formula,
    string Name,
    ParticipantRole Role,
    int Coefficient,
    MatterState? State);
