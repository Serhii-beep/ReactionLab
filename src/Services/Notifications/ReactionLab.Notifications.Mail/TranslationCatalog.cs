using System.Collections.Frozen;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Jeffijoe.MessageFormat;
using Jeffijoe.MessageFormat.Parsing;

namespace ReactionLab.Notifications.Mail;

internal sealed class TranslationCatalog : ITranslationCatalog
{
    public const string DefaultLocale = "en";

    private const string ResourcePrefix = "locales/";
    private const string ResourceSuffix = "/mail.json";
    private const string OtherBranch = "other";

    private static readonly IReadOnlyDictionary<string, object?> NoArguments = new Dictionary<string, object?>();

    private readonly MessageFormatter _formatter = new(useCache: true);
    private readonly KeyedBlockReader _blocks = new();
    private readonly FrozenDictionary<string, FrozenDictionary<string, string>> _patternsByLocale;

    public TranslationCatalog() => _patternsByLocale = Load(typeof(TranslationCatalog).Assembly);

    public IReadOnlyCollection<string> Locales => _patternsByLocale.Keys;

    public string ResolveLocale(string? requestedLocale)
    {
        if (string.IsNullOrWhiteSpace(requestedLocale))
        {
            return DefaultLocale;
        }

        if (_patternsByLocale.ContainsKey(requestedLocale))
        {
            return requestedLocale;
        }

        var language = requestedLocale.Split('-')[0];

        return _patternsByLocale.ContainsKey(language) ? language : DefaultLocale;
    }

    public string Translate(string locale, string key, IReadOnlyDictionary<string, object?>? arguments = null)
    {
        var pattern = FindPattern(locale, key)
            ?? FindPattern(DefaultLocale, key)
            ?? throw new KeyNotFoundException($"No mail translation has the key '{key}'.");

        return _formatter.FormatMessage(pattern, arguments ?? NoArguments, CultureInfo.GetCultureInfo(locale));
    }

    private string? FindPattern(string locale, string key) =>
        _patternsByLocale.TryGetValue(locale, out var patterns) && patterns.TryGetValue(key, out var pattern)
            ? pattern
            : null;

    private FrozenDictionary<string, FrozenDictionary<string, string>> Load(Assembly assembly)
    {
        var patternsByLocale = ReadPatterns(assembly);

        if (!patternsByLocale.TryGetValue(DefaultLocale, out var defaultPatterns))
        {
            throw new InvalidOperationException($"The '{DefaultLocale}' mail translations aren't embedded.");
        }

        var defaultArguments = defaultPatterns.ToDictionary(
            entry => entry.Key,
            entry => ReadArguments(DefaultLocale, entry.Key, entry.Value),
            StringComparer.Ordinal);

        foreach (var (locale, patterns) in patternsByLocale.Where(entry => entry.Key != DefaultLocale))
        {
            foreach (var (key, pattern) in patterns)
            {
                var arguments = ReadArguments(locale, key, pattern);

                if (!defaultArguments.TryGetValue(key, out var expected))
                {
                    throw new InvalidOperationException(
                        $"The {locale} mail translation '{key}' has no {DefaultLocale} original.");
                }

                if (!arguments.SetEquals(expected))
                {
                    throw new InvalidOperationException(
                        $"The {locale} mail translation '{key}' doesn't take the {DefaultLocale} one's arguments.");
                }
            }
        }

        return patternsByLocale.ToFrozenDictionary(
            entry => entry.Key,
            entry => entry.Value.ToFrozenDictionary(StringComparer.Ordinal),
            StringComparer.OrdinalIgnoreCase);
    }

    private HashSet<string> ReadArguments(string locale, string key, string pattern)
    {
        if (pattern.Any(character =>
            char.IsControl(character) || char.GetUnicodeCategory(character) == UnicodeCategory.PrivateUse))
        {
            throw new InvalidOperationException(
                $"The {locale} mail translation '{key}' holds a control or private-use character.");
        }

        var arguments = new HashSet<string>(StringComparer.Ordinal);

        try
        {
            CollectArguments(pattern, arguments);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            throw new InvalidOperationException($"The {locale} mail translation '{key}' doesn't parse.", ex);
        }

        return arguments;
    }

    private void CollectArguments(string pattern, HashSet<string> arguments)
    {
        foreach (var request in new PatternParser().Parse(new StringBuilder(pattern)))
        {
            _formatter.Formatters.GetFormatter(request);
            arguments.Add(request.Variable);

            if (request.FormatterName is not ("plural" or "selectordinal" or "select"))
            {
                continue;
            }

            var branches = _blocks.Read(request).ToList();

            if (branches.All(branch => branch.Key != OtherBranch))
            {
                throw new InvalidOperationException($"A {request.FormatterName} has no '{OtherBranch}' branch.");
            }

            foreach (var branch in branches)
            {
                CollectArguments(branch.BlockText, arguments);
            }
        }
    }

    private static Dictionary<string, Dictionary<string, string>> ReadPatterns(Assembly assembly)
    {
        var patternsByLocale = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            if (!resourceName.StartsWith(ResourcePrefix, StringComparison.Ordinal)
                || !resourceName.EndsWith(ResourceSuffix, StringComparison.Ordinal))
            {
                continue;
            }

            using var stream = assembly.GetManifestResourceStream(resourceName)!;
            using var document = JsonDocument.Parse(stream);
            var patterns = new Dictionary<string, string>(StringComparer.Ordinal);

            Flatten(document.RootElement, keyPrefix: null, patterns);
            patternsByLocale.Add(resourceName[ResourcePrefix.Length..^ResourceSuffix.Length], patterns);
        }

        return patternsByLocale;
    }

    private static void Flatten(JsonElement element, string? keyPrefix, Dictionary<string, string> patterns)
    {
        foreach (var property in element.EnumerateObject())
        {
            var key = keyPrefix is null ? property.Name : $"{keyPrefix}.{property.Name}";

            switch (property.Value.ValueKind)
            {
                case JsonValueKind.Object:
                    Flatten(property.Value, key, patterns);
                    break;
                case JsonValueKind.String:
                    patterns.Add(key, property.Value.GetString()!);
                    break;
                default:
                    throw new InvalidOperationException($"The mail translation '{key}' isn't text.");
            }
        }
    }
}
