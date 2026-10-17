using ReactionLab.SharedKernel;

namespace ReactionLab.Catalog.Application.Features.Reactions.GetReactionById;

public sealed record GetReactionByIdQuery(Guid Id, SupportedLocale Locale);
