using ReactionLab.SharedKernel;

namespace ReactionLab.Catalog.Application.Features.Substances.GetSubstanceById;

public sealed record GetSubstanceByIdQuery(Guid Id, SupportedLocale Locale);
