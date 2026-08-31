using ReactionLab.Domain.Enums;
using ReactionLab.Domain.Reactions;

namespace ReactionLab.Infrastructure.Persistence.Seeding;

internal sealed record ReactionSeed(
    string Key,
    ReactionContent Content,
    ReactionType Type,
    DifficultyLevel Difficulty,
    bool IsReversible,
    IReadOnlyList<ParticipantSeed> Participants,
    Thermodynamics? Energetics,
    ReactionConditions? Conditions,
    VisualizationHint? Visualization,
    ReactionProvenance Provenance,
    IReadOnlyList<string> Tags);
