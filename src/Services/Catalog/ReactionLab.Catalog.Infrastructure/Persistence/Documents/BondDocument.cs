using ReactionLab.Catalog.Domain.Substances;

namespace ReactionLab.Catalog.Infrastructure.Persistence.Documents;

internal sealed record BondDocument(int From, int To, BondType Type);
