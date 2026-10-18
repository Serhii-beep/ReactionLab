using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ReactionLab.Catalog.Application.Common.Abstractions;
using ReactionLab.Catalog.Application.Features.Chemistry;
using ReactionLab.Catalog.Application.Features.Elements;
using ReactionLab.Catalog.Application.Features.Reactions;
using ReactionLab.Catalog.Application.Features.Substances;
using ReactionLab.SharedKernel;

namespace ReactionLab.Catalog.Application.Features.Releases;

public sealed class CatalogDocumentBuilder(IAppDbContext context)
{
    public async Task<CatalogDocument> BuildAsync(SupportedLocale locale, CancellationToken cancellationToken)
    {
        var elements = await ElementQueries.ResponsesAsync(
            context.Elements.AsNoTracking().OrderBy(element => element.AtomicNumber),
            locale,
            cancellationToken);

        var substances = await SubstanceQueries.ResponsesAsync(
            context.Substances.AsNoTracking().OrderBy(substance => substance.Id),
            locale,
            cancellationToken);

        var reactions = await ReactionQueries.ResponsesAsync(
            context.Reactions.AsNoTracking().OrderBy(reaction => reaction.Id),
            context.Substances.AsNoTracking(),
            locale,
            cancellationToken);

        var reference = await ChemistryQueries.DocumentAsync(context.ChemistryReferences.AsNoTracking(), cancellationToken);

        using var parsed = JsonDocument.Parse(reference);

        return new CatalogDocument(elements, substances, reactions, parsed.RootElement.Clone());
    }
}
