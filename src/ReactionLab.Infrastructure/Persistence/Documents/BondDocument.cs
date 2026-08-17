using ReactionLab.Domain.Substances;

namespace ReactionLab.Infrastructure.Persistence.Documents;

internal sealed record BondDocument(int From, int To, BondType Type);
