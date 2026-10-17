using ReactionLab.Catalog.Domain.Substances;

namespace ReactionLab.Catalog.Application.Features.Substances.Contracts;

public sealed record BondResponse(int FromAtomIndex, int ToAtomIndex, BondType Type);
