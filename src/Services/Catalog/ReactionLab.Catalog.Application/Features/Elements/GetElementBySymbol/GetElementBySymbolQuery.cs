using ReactionLab.SharedKernel;

namespace ReactionLab.Catalog.Application.Features.Elements.GetElementBySymbol;

public sealed record GetElementBySymbolQuery(string Symbol, SupportedLocale Locale);
