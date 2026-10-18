using ReactionLab.SharedKernel;

namespace ReactionLab.Catalog.Application.Features.Releases;

public sealed record ReleaseRequest(Guid ReleaseId, SupportedLocale Locale);
