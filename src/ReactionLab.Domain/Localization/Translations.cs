using ReactionLab.Domain.Common;

namespace ReactionLab.Domain.Localization;

public static class Translations
{
    public static readonly Error DefaultContentRequired = Error.Validation(
        "Translations.DefaultContentRequired",
        "Content for the default locale is required.");

    public static Result<Translations<TContent>> Create<TContent>(TContent? defaultContent)
        where TContent : class, ITranslatableContent<TContent>
    {
        if (defaultContent is null)
        {
            return DefaultContentRequired;
        }

        var byLocale = new Dictionary<SupportedLocale, TContent>
        {
            [SupportedLocale.Default] = defaultContent
        };

        return new Translations<TContent>(byLocale);
    }
}

public sealed class Translations<TContent>
    where TContent : class, ITranslatableContent<TContent>
{
    private readonly Dictionary<SupportedLocale, TContent> _byLocale;

    internal Translations(Dictionary<SupportedLocale, TContent> byLocale)
    {
        _byLocale = byLocale;
        Locales = SupportedLocale.All.Where(byLocale.ContainsKey).ToList();
    }

    public IReadOnlyList<SupportedLocale> Locales { get; }

    public Translations<TContent> With(SupportedLocale locale, TContent content)
    {
        var updated = new Dictionary<SupportedLocale, TContent>(_byLocale)
        {
            [locale] = content
        };

        return new Translations<TContent>(updated);
    }

    public bool Has(SupportedLocale locale) => _byLocale.ContainsKey(locale);

    public TContent Resolve(SupportedLocale locale)
    {
        var fallback = _byLocale[SupportedLocale.Default];

        if (locale == SupportedLocale.Default)
        {
            return fallback;
        }

        return _byLocale.TryGetValue(locale, out var content)
            ? content.WithFallback(fallback)
            : fallback;
    }
}
