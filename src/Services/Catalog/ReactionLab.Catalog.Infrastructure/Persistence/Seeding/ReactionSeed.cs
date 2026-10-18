using ReactionLab.Catalog.Domain.Enums;
using ReactionLab.Catalog.Domain.Reactions;
using ReactionLab.SharedKernel;

namespace ReactionLab.Catalog.Infrastructure.Persistence.Seeding;

internal sealed record ReactionSeed(
    string Key,
    Translations<ReactionContent> Translations,
    ReactionType Type,
    DifficultyLevel Difficulty,
    bool IsReversible,
    IReadOnlyList<ParticipantSeed> Participants,
    Thermodynamics? Energetics,
    ReactionConditions? Conditions,
    VisualizationHint? Visualization,
    ReactionProvenance Provenance,
    IReadOnlyList<string> Tags);
