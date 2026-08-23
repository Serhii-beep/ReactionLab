using ReactionLab.Domain.Localization;

namespace ReactionLab.Application.Features.Elements.ListElements;

public sealed record ListElementsQuery(string? Search, SupportedLocale Locale);
