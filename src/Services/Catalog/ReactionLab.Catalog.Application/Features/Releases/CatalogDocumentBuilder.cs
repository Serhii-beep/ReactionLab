using Microsoft.EntityFrameworkCore;
using ReactionLab.Catalog.Application.Common.Abstractions;
using ReactionLab.Catalog.Application.Features.Chemistry;
using ReactionLab.Catalog.Application.Features.Elements;
using ReactionLab.Catalog.Application.Features.Elements.Contracts;
using ReactionLab.SharedKernel;

namespace ReactionLab.Catalog.Application.Features.Releases;

public sealed class CatalogDocumentBuilder(IAppDbContext context)
{
    public async Task<IReadOnlyList<ElementResponse>> ElementsAsync(SupportedLocale locale, CancellationToken cancellationToken) =>
        await ElementQueries.ResponsesAsync(
            context.Elements.AsNoTracking().OrderBy(element => element.AtomicNumber),
            locale,
            cancellationToken);

    public async Task<string> ReferenceAsync(CancellationToken cancellationToken) =>
        await ChemistryQueries.DocumentAsync(context.ChemistryReferences.AsNoTracking(), cancellationToken);
}
