using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ReactionLab.Domain.Elements;
using ReactionLab.Domain.Reactions;
using ReactionLab.Domain.Substances;

namespace ReactionLab.Infrastructure.Persistence.Interceptors;

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
            entry.Property<string>(PersistenceColumns.SearchText).CurrentValue = SearchTextFor(entry.Entity);
        }

        foreach (var entry in context.ChangeTracker.Entries<Reaction>().Where(IsWrite))
        {
            entry.Property<string>(PersistenceColumns.SearchText).CurrentValue = SearchTextFor(entry.Entity);

            entry.Property<Guid[]>(PersistenceColumns.ReactantSignature).CurrentValue =
                [.. entry.Entity.ReactantSignature.Select(id => id.Value)];
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

    public static string SearchTextFor(Reaction reaction) =>
        Join(reaction.Locales.Select(locale => reaction.Content(locale).Name));

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
