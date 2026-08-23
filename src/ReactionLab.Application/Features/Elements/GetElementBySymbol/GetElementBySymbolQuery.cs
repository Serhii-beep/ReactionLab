using ReactionLab.Domain.Localization;

namespace ReactionLab.Application.Features.Elements.GetElementBySymbol;

public sealed record GetElementBySymbolQuery(string Symbol, SupportedLocale Locale);
