using ReactionLab.Domain.Localization;

namespace ReactionLab.Application.Features.Substances.GetSubstanceById;

public sealed record GetSubstanceByIdQuery(Guid Id, SupportedLocale Locale);
