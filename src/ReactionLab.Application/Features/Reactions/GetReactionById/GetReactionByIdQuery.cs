using ReactionLab.Domain.Localization;

namespace ReactionLab.Application.Features.Reactions.GetReactionById;

public sealed record GetReactionByIdQuery(Guid Id, SupportedLocale Locale);
