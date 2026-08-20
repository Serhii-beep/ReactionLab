using ReactionLab.Domain.Elements;
using ReactionLab.Domain.Substances;

namespace ReactionLab.Infrastructure.Persistence.Seeding;

internal sealed record CatalogBatch(
    IReadOnlyList<Element> Elements,
    IReadOnlyList<Substance> Substances,
    IReadOnlyList<ReactionSeed> Reactions,
    IReadOnlyList<string> Rejections);
