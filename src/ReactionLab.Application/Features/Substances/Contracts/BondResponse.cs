using ReactionLab.Domain.Substances;

namespace ReactionLab.Application.Features.Substances.Contracts;

public sealed record BondResponse(int FromAtomIndex, int ToAtomIndex, BondType Type);
