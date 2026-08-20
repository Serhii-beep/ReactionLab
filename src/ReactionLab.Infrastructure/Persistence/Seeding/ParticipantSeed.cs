using ReactionLab.Domain.Enums;

namespace ReactionLab.Infrastructure.Persistence.Seeding;

internal sealed record ParticipantSeed(
    string Formula,
    ParticipantRole Role,
    int Coefficient,
    MatterState? State,
    string? Substance);
