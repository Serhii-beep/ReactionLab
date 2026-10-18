namespace ReactionLab.Catalog.Infrastructure.Persistence.Seeding;

internal sealed record ImportCounts(
    int StagedSubstances,
    int RekeyedSubstances,
    int WrittenSubstances,
    int RetiredSubstances,
    int StagedReactions,
    int WrittenReactions,
    int RetiredReactions,
    int Duplicates);
