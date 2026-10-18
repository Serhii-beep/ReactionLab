using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ReactionLab.Catalog.Domain.Elements;
using ReactionLab.Catalog.Domain.Reactions;
using ReactionLab.Catalog.Domain.Substances;
using ReactionLab.SharedKernel;

namespace ReactionLab.Catalog.Infrastructure.Persistence.Interceptors;

internal sealed class SearchProjectionInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Project(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Project(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public static void Project(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        foreach (var entry in context.ChangeTracker.Entries<Element>().Where(IsWrite))
        {
            entry.Property<string>(PersistenceColumns.SearchText).CurrentValue = SearchTextFor(entry.Entity);
        }

        foreach (var entry in context.ChangeTracker.Entries<Substance>().Where(IsWrite))
        {
            var structureKey = StructureKeys.Of(entry.Entity.Structure);

            entry.Property<string>(PersistenceColumns.SearchText).CurrentValue = SearchTextFor(entry.Entity);
            entry.Property<string[]>(PersistenceColumns.ElementSymbols).CurrentValue = ElementSymbolFor(entry.Entity);
            entry.Property<string?>(PersistenceColumns.StructureKey).CurrentValue = structureKey;

            var naturalKey = entry.Property<string>(PersistenceColumns.NaturalKey);

            if (string.IsNullOrEmpty(naturalKey.CurrentValue))
            {
                naturalKey.CurrentValue = NaturalKeys.ForSubstance(
                    pubChemCid: null,
                    structureKey,
                    entry.Entity.Formula.Value,
                    entry.Entity.Content(SupportedLocale.Default).Name);
            }
        }

        foreach (var entry in context.ChangeTracker.Entries<Reaction>().Where(IsWrite))
        {
            entry.Property<string>(PersistenceColumns.SearchText).CurrentValue = SearchTextFor(entry.Entity);

            entry.Property<Guid[]>(PersistenceColumns.ReactantSignature).CurrentValue =
                [.. entry.Entity.ReactantSignature.Select(id => id.Value)];

            var naturalKey = entry.Property<string>(PersistenceColumns.NaturalKey);

            if (string.IsNullOrEmpty(naturalKey.CurrentValue))
            {
                naturalKey.CurrentValue = NaturalKeys.ForReaction(
                    entry.Entity.Participants.Select(participant => new NaturalKeys.ReactionSide(
                        participant.SubstanceId.Value, participant.Role, participant.Coefficient)),
                    entry.Entity.Provenance.Rule);
            }
        }
    }

    public static string SearchTextFor(Element element) =>
        Join([element.Symbol.Value, .. element.Locales.Select(locale => element.Content(locale).Name)]);

    public static string SearchTextFor(Substance substance) =>
        Join([
            substance.Formula.Value,
            substance.Formula.Hill,
            .. substance.Locales.SelectMany(locale =>
            {
                var content = substance.Content(locale);

                return new[] { content.Name, content.IupacName }.Concat(content.CommonNames);
            })
        ]);

    public static string SearchTextFor(Reaction reaction) => SearchTextFor(reaction.Translations);

    public static string SearchTextFor(Translations<ReactionContent> translations) =>
        Join(translations.Locales.Select(locale => translations.Resolve(locale).Name));

    public static string[] ElementSymbolFor(Substance substance) =>
        [.. substance.Formula.Composition.Select(quantity => quantity.Symbol.Value).Distinct().Order()];

    private static bool IsWrite<TEntity>(EntityEntry<TEntity> entry)
        where TEntity : class =>
        entry.State is EntityState.Added or EntityState.Modified;

    private static string Join(IEnumerable<string?> parts) =>
        string.Join(
            ' ',
            parts.Where(part => !string.IsNullOrWhiteSpace(part))
                .Select(part => part!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase));
}
