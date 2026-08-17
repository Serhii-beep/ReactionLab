namespace ReactionLab.Domain.Common;

internal static class TextNormalizer
{
    public static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static string? Clean(string? value, int maximumLength)
    {
        var cleaned = Clean(value);

        return cleaned is null || cleaned.Length <= maximumLength
            ? cleaned
            : cleaned[..maximumLength];
    }

    public static IReadOnlyList<string> CleanAll(IEnumerable<string>? values) =>
        values is null
            ? []
            : values
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .ToList()
                .AsReadOnly();
}
