using System.Security.Cryptography;

namespace ReactionLab.Identity.Api.Passwords;

internal static class GeneratedPasswords
{
    private const string Consonants = "bdfghjklmnprstvz";
    private const string Vowels = "aeiou";
    private const int WordCount = 4;

    public static string New() =>
        string.Join('-', Enumerable.Range(0, WordCount).Select(_ => string.Concat(
            RandomNumberGenerator.GetString(Consonants, 1),
            RandomNumberGenerator.GetString(Vowels, 1),
            RandomNumberGenerator.GetString(Consonants, 1),
            RandomNumberGenerator.GetString(Vowels, 1))));
}
