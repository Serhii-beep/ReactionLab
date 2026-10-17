using ReactionLab.SharedKernel;

namespace ReactionLab.Catalog.Application.Features.Elements.GetElementById;

public sealed record GetElementByIdQuery(Guid Id, SupportedLocale Locale);
