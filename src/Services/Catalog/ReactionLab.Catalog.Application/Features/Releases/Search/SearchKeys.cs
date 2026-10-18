using System.Text;

namespace ReactionLab.Catalog.Application.Features.Releases.Search;

public static class SearchKeys
{
    public const int MinimumLength = 2;

    public const int MaximumLength = 100;

    public const int MaximumPrefixLength = 3;

    public static string Normalize(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var builder = new StringBuilder(text.Length);
        var pendingSpace = false;

        foreach (var rune in text.Normalize(NormalizationForm.FormC).EnumerateRunes())
        {
            if (Rune.IsWhiteSpace(rune))
            {
                pendingSpace = builder.Length > 0;

                continue;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(Rune.ToLowerInvariant(rune).ToString());
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    public static bool IsNormalized(string text) =>
        string.Equals(text, Normalize(text), StringComparison.Ordinal);

    public static IEnumerable<string> Words(string key)
    {
        var word = new StringBuilder();

        foreach (var rune in key.EnumerateRunes())
        {
            if (Rune.IsLetterOrDigit(rune))
            {
                word.Append(rune.ToString());
            }
            else if (word.Length > 0)
            {
                yield return word.ToString();
                word.Clear();
            }
        }

        if (word.Length > 0)
        {
            yield return word.ToString();
        }
    }

    public static string? LongestWord(string key) => Words(key).MaxBy(LengthOf);

    public static IEnumerable<string> Buckets(string word)
    {
        var runes = word.EnumerateRunes().ToArray();

        for (var length = MinimumLength; length <= Math.Min(MaximumPrefixLength, runes.Length); length++)
        {
            yield return string.Concat(runes.Take(length).Select(rune => rune.ToString()));
        }

        if (runes.Length > MaximumPrefixLength && runes.Length <= MaximumLength)
        {
            yield return word;
        }
    }

    public static int LengthOf(string key) => key.EnumerateRunes().Count();
}
