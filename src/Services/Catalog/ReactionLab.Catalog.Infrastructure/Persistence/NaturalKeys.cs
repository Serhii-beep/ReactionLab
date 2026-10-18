using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ReactionLab.Catalog.Domain.Enums;

namespace ReactionLab.Catalog.Infrastructure.Persistence;

internal static class NaturalKeys
{
    public const int MaximumLength = 400;

    private const string HashedPrefix = "sha256:";

    public static string ForSubstance(int? pubChemCid, string? structureKey, string formula, string englishName)
    {
        if (pubChemCid is { } cid)
        {
            return string.Create(CultureInfo.InvariantCulture, $"cid:{cid}");
        }

        return string.IsNullOrEmpty(structureKey) ? FormulaKeyOf(formula, englishName) : StructureKeyOf(structureKey);
    }

    public static string[] WeakerKeysForSubstance(int? pubChemCid, string? structureKey, string formula, string englishName)
    {
        var hasStructure = !string.IsNullOrEmpty(structureKey);

        return (pubChemCid, hasStructure) switch
        {
            (not null, true) => [StructureKeyOf(structureKey!), FormulaKeyOf(formula, englishName)],
            (not null, false) => [FormulaKeyOf(formula, englishName)],
            (null, true) => [FormulaKeyOf(formula, englishName)],
            _ => []
        };
    }

    public static string ForReaction(IEnumerable<ReactionSide> participants, string rule)
    {
        var sides = participants.ToLookup(participant => participant.Role);

        return Bounded($"{Side(sides[ParticipantRole.Reactant])}=>{Side(sides[ParticipantRole.Product])}|{rule}");
    }

    private static string StructureKeyOf(string structureKey) => $"structure:{structureKey}";

    private static string FormulaKeyOf(string formula, string englishName) =>
        Bounded($"formula:{formula}|{englishName.Trim().ToLowerInvariant()}");

    private static string Side(IEnumerable<ReactionSide> participants) =>
        string.Join(
            '+',
            participants
                .OrderBy(participant => participant.SubstanceId)
                .Select(participant => string.Create(
                    CultureInfo.InvariantCulture,
                    $"{participant.Coefficient}*{participant.SubstanceId:N}")));

    private static string Bounded(string key) =>
        key.Length <= MaximumLength
            ? key
            : HashedPrefix + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(key)));

    internal readonly record struct ReactionSide(Guid SubstanceId, ParticipantRole Role, int Coefficient);
}
