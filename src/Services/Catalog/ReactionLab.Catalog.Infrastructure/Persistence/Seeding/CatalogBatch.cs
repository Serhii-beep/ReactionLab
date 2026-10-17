using ReactionLab.Catalog.Domain.Elements;
using ReactionLab.Catalog.Domain.Substances;

namespace ReactionLab.Catalog.Infrastructure.Persistence.Seeding;

internal sealed record CatalogBatch(
    IReadOnlyList<Element> Elements,
    IReadOnlyList<Substance> Substances,
    IReadOnlyList<ReactionSeed> Reactions,
    IReadOnlyList<string> Rejections);
