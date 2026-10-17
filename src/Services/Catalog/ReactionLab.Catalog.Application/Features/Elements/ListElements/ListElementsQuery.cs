using ReactionLab.SharedKernel;

namespace ReactionLab.Catalog.Application.Features.Elements.ListElements;

public sealed record ListElementsQuery(string? Search, SupportedLocale Locale);
