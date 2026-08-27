using Microsoft.EntityFrameworkCore;
using ReactionLab.Application.Common.Abstractions;
using ReactionLab.Application.Common.Caching;
using ReactionLab.Domain.Common;
using ReactionLab.Domain.Elements;
using ReactionLab.Domain.Localization;

namespace ReactionLab.Application.Features.Elements.TranslateElement;

public sealed class TranslateElementHandler(IAppDbContext context, CacheInvalidator invalidator)
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
                .. SupportedLocale.All.Select(locale => CacheKeys.Element(command.ElementId, locale)),
                .. SupportedLocale.All.Select(locale => CacheKeys.ElementBySymbol(element.Symbol.Value, locale))
            ],
            CacheTags.Elements,
            cancellationToken);

        return Result.Success();
    }
}
