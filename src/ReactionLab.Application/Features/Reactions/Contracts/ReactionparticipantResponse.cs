using ReactionLab.Domain.Enums;

namespace ReactionLab.Application.Features.Reactions.Contracts;

public sealed record ReactionparticipantResponse(
    Guid SubstanceId,
    string Formula,
    string Name,
    ParticipantRole Role,
    int Coefficient,
    MatterState? State);
