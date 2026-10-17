using ReactionLab.Catalog.Domain.Enums;

namespace ReactionLab.Catalog.Infrastructure.Persistence.Seeding;

internal sealed record ParticipantSeed(
    string Formula,
    ParticipantRole Role,
    int Coefficient,
    MatterState? State,
    string? Substance);
