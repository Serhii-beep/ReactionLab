using Microsoft.EntityFrameworkCore;
using ReactionLab.Catalog.Application.Common.Abstractions;
using ReactionLab.Catalog.Application.Features.Reactions.Contracts;
using ReactionLab.Catalog.Domain.Enums;
using ReactionLab.Catalog.Domain.Reactions;
using ReactionLab.Catalog.Domain.Substances;
using ReactionLab.SharedKernel;

namespace ReactionLab.Catalog.Application.Features.Reactions;

internal static class ReactionQueries
{
    public static async Task<ReactionResponse?> FirstResponseAsync(
        IQueryable<Reaction> reactions,
        IQueryable<Substance> substances,
        SupportedLocale locale,
        CancellationToken cancellationToken)
    {
        var responses = await ResponsesAsync(reactions.OrderBy(reaction => reaction.Id).Take(1), substances, locale, cancellationToken);

        return responses.Count > 0 ? responses[0] : null;
    }

    public static async Task<IReadOnlyList<ReactionResponse>> ResponsesAsync(
        IQueryable<Reaction> reactions,
        IQueryable<Substance> substances,
        SupportedLocale locale,
        CancellationToken cancellationToken)
    {
        var rows = await reactions
            .Select(reaction => new
            {
                reaction.Id,
                reaction.Type,
                reaction.Difficulty,
                reaction.IsReversible,
                reaction.Energetics,
                reaction.Conditions,
                reaction.Visualization,
                reaction.Provenance,
                reaction.Translations,
                Tags = EF.Property<List<string>>(reaction, PersistedFields.ReactionTags),
                Participants = reaction.Participants
                    .Select(participant => new ParticipantRow(
                        participant.SubstanceId,
                        participant.Role,
                        participant.Coefficient,
                        participant.State))
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        var names = await NamesAsync(substances, rows.SelectMany(row => row.Participants), cancellationToken);

        return rows.ConvertAll(row =>
        {
            var content = row.Translations.Resolve(locale);

            return new ReactionResponse(
                row.Id.Value,
                content.Name,
                content.Description,
                content.Mechanism,
                content.SafetyWarnings,
                content.RealWorldExamples,
                row.Type,
                row.Difficulty.Value,
                row.IsReversible,
                Map(row.Participants, names, locale),
                row.Energetics.EnthalpyChange?.KilojoulesPerMole,
                row.Energetics.ActivationEnergyKilojoulesPerMole,
                row.Energetics.ReverseActivationEnergyKilojoulesPerMole,
                row.Energetics.IsExothermic,
                row.Conditions.Temperature?.Kelvin,
                row.Conditions.Pressure?.Kilopascals,
                row.Conditions.Catalyst,
                row.Visualization.PresetKey,
                row.Visualization.DurationMilliseconds,
                row.Tags,
                row.Provenance.Rule,
                row.Provenance.Confidence);
        });
    }

    public static async Task<IReadOnlyList<ReactionSummaryResponse>> SummariesAsync(
        IQueryable<Reaction> reactions,
        IQueryable<Substance> substances,
        SupportedLocale locale,
        CancellationToken cancellationToken)
    {
        var rows = await reactions
            .Select(reaction => new
            {
                reaction.Id,
                reaction.Type,
                reaction.Difficulty,
                reaction.IsReversible,
                reaction.Energetics,
                reaction.Visualization,
                reaction.Conditions,
                reaction.Translations,
                Tags = EF.Property<List<string>>(reaction, PersistedFields.ReactionTags),
                Participants = reaction.Participants
                    .Select(participant => new ParticipantRow(
                        participant.SubstanceId,
                        participant.Role,
                        participant.Coefficient,
                        participant.State))
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        var names = await NamesAsync(substances, rows.SelectMany(row => row.Participants), cancellationToken);

        return rows.ConvertAll(row => new ReactionSummaryResponse(
            row.Id.Value,
            row.Translations.Resolve(locale).Name,
            row.Type,
            row.Difficulty.Value,
            row.IsReversible,
            row.Energetics.EnthalpyChange?.KilojoulesPerMole,
            row.Energetics.ActivationEnergyKilojoulesPerMole,
            row.Energetics.IsExothermic,
            row.Visualization.PresetKey,
            row.Visualization.DurationMilliseconds,
            row.Conditions.Temperature?.Kelvin,
            row.Conditions.Pressure?.Kilopascals,
            row.Conditions.Catalyst,
            row.Tags,
            Map(row.Participants, names, locale)));
    }

    private static async Task<Dictionary<SubstanceId, SubstanceName>> NamesAsync(
        IQueryable<Substance> substances,
        IEnumerable<ParticipantRow> participants,
        CancellationToken cancellationToken)
    {
        var ids = participants.Select(participant => participant.SubstanceId).Distinct().ToList();

        if (ids.Count == 0)
        {
            return [];
        }

        var rows = await substances
            .Where(substance => ids.Contains(substance.Id))
            .Select(substance => new SubstanceName(substance.Id, substance.Formula, substance.Translations))
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(row => row.Id);
    }

    private static List<ReactionParticipantResponse> Map(
        List<ParticipantRow> rows,
        Dictionary<SubstanceId, SubstanceName> names,
        SupportedLocale locale) =>
        [.. rows
            .Where(row => names.ContainsKey(row.SubstanceId))
            .Select(row =>
            {
                var substance = names[row.SubstanceId];

                return new ReactionParticipantResponse(
                    row.SubstanceId.Value,
                    substance.Formula.Value,
                    substance.Translations.Resolve(locale).Name,
                    row.Role,
                    row.Coefficient,
                    row.State);
            })];

    private sealed record ParticipantRow(
        SubstanceId SubstanceId,
        ParticipantRole Role,
        int Coefficient,
        MatterState? State);

    private sealed record SubstanceName(
        SubstanceId Id,
        ChemicalFormula Formula,
        Translations<SubstanceContent> Translations);
}
