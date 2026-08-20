using ReactionLab.Domain.Localization;

namespace ReactionLab.Application.Features.Elements.GetElementById;

public sealed record GetElementByIdQuery(Guid Id, SupportedLocale Locale);
