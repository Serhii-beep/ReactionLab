using Microsoft.EntityFrameworkCore;
using ReactionLab.Catalog.Application.Common.Abstractions;
using ReactionLab.Catalog.Application.Common.Caching;
using ReactionLab.Catalog.Domain.Elements;
using ReactionLab.SharedKernel;

namespace ReactionLab.Catalog.Application.Features.Elements.TranslateElement;

public sealed class TranslateElementHandler(IAppDbContext context, CacheInvalidator invalidator, CatalogCacheScope scope)
    : ICommandHandler<TranslateElementCommand>
{
    public async ValueTask<Result> HandleAsync(
        TranslateElementCommand command,
        CancellationToken cancellationToken)
    {
        var locale = SupportedLocale.Create(command.Locale);
        if (locale.IsFailure)
        {
            return Result.Failure(locale.Error);
        }

        var content = ElementContent.Create(command.Name, command.DiscoveryInfo, command.InterestingFacts);

        if (content.IsFailure)
        {
            return Result.Failure(content.Error);
        }

        var id = ElementId.From(command.ElementId);

        var element = await context.Elements.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (element is null)
        {
            return Result.Failure(ElementErrors.NotFound(command.ElementId));
        }

        var translated = element.Translate(locale.Value, content.Value);
        if (translated.IsFailure)
        {
            return translated;
        }

        await context.SaveChangesAsync(cancellationToken);

        await invalidator.InvalidateAsync(
            [
                .. SupportedLocale.All.Select(locale => CacheKeys.Element(scope.Name, command.ElementId, locale)),
                .. SupportedLocale.All.Select(locale => CacheKeys.ElementBySymbol(scope.Name, element.Symbol.Value, locale))
            ],
            CacheTags.Elements,
            cancellationToken);

        return Result.Success();
    }
}
