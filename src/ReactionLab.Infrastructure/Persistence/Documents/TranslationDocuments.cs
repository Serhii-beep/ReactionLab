using System.Text.Json;
using ReactionLab.Domain.Localization;

namespace ReactionLab.Infrastructure.Persistence.Documents;

internal static class TranslationDocuments
{
    public static string Serialize<TContent, TDocument>(
        Translations<TContent> translations,
        Func<TContent, TDocument> toDocument)
        where TContent : class, ITranslatableContent<TContent>
    {
        var documents = new Dictionary<string, TDocument>(StringComparer.Ordinal);

        foreach (var locale in translations.Locales)
        {
            if (translations.Stored(locale) is { } content)
            {
                documents[locale.Code] = toDocument(content);
            }
        }

        return JsonSerializer.Serialize(documents, PersistenceJson.Options);
    }

    public static Translations<TContent> Deserialize<TContent, TDocument>(
        string json,
        Func<TDocument, TContent> toContent)
        where TContent : class, ITranslatableContent<TContent>
    {
        var documents = JsonSerializer.Deserialize<Dictionary<string, TDocument>>(json, PersistenceJson.Options)
            ?? throw new InvalidOperationException("Stored translations were not a JSON object.");

        if (!documents.TryGetValue(SupportedLocale.Default.Code, out var defaultDocument))
        {
            throw new InvalidOperationException($"Stored translations are missing the default locale '{SupportedLocale.Default.Code}'.");
        }

        var translations = PersistenceJson.Require(Translations.Create(toContent(defaultDocument)), "translations");

        foreach (var (code, document) in documents)
        {
            if (string.Equals(code, SupportedLocale.Default.Code, StringComparison.Ordinal))
            {
                continue;
            }

            var locale = SupportedLocale.Create(code);
            if (locale.IsFailure)
            {
                continue;
            }

            translations = translations.With(locale.Value, toContent(document));
        }

        return translations;
    }
}
