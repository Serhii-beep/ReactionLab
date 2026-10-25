using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Persistence.Accounts;

public static partial class DisplayNames
{
    private const int ZeroWidthNonJoiner = 0x200C;
    private const int ZeroWidthJoiner = 0x200D;
    private const int MaximumCombiningMarksInARow = 4;

    private static readonly int[] BlankLookingCharacters =
        [0x034F, 0x115F, 0x1160, 0x17B4, 0x17B5, 0x2800, 0x3164, 0xFFA0];

    public static Result<string> Normalize(
        string? name,
        int maximumLength,
        Error required,
        Error tooLong,
        Error invalid)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return required;
        }

        if (!UnicodeText.TryNormalize(WhitespaceRun().Replace(name.Trim(), " "), out var normalized))
        {
            return invalid;
        }

        var characters = normalized.EnumerateRunes().ToList();

        if (characters.Count > maximumLength)
        {
            return tooLong;
        }

        return characters.Any(IsHiddenOrUnassigned) || !characters.Any(IsVisibleSign) || HasStackedMarks(characters)
            ? invalid
            : normalized;
    }

    private static bool IsHiddenOrUnassigned(Rune character) =>
        BlankLookingCharacters.Contains(character.Value) || Rune.GetUnicodeCategory(character) switch
        {
            UnicodeCategory.Control or
            UnicodeCategory.LineSeparator or
            UnicodeCategory.ParagraphSeparator or
            UnicodeCategory.PrivateUse or
            UnicodeCategory.OtherNotAssigned => true,
            UnicodeCategory.Format => character.Value is not (ZeroWidthNonJoiner or ZeroWidthJoiner),
            _ => false
        };

    private static bool IsVisibleSign(Rune character) =>
        Rune.IsLetterOrDigit(character) || Rune.IsSymbol(character);

    private static bool HasStackedMarks(List<Rune> characters)
    {
        var marksInARow = 0;

        foreach (var character in characters)
        {
            marksInARow = IsCombiningMark(character) ? marksInARow + 1 : 0;

            if (marksInARow > MaximumCombiningMarksInARow)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsCombiningMark(Rune character) =>
        Rune.GetUnicodeCategory(character) is UnicodeCategory.NonSpacingMark
            or UnicodeCategory.SpacingCombiningMark
            or UnicodeCategory.EnclosingMark;

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRun();
}
