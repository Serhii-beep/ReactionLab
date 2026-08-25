using Microsoft.EntityFrameworkCore;
using ReactionLab.Application.Features.Reactions.Contracts;
using ReactionLab.Domain.Enums;
using ReactionLab.Domain.Localization;
using ReactionLab.Domain.Reactions;
using ReactionLab.Domain.Substances;

namespace ReactionLab.Application.Features.Reactions;

internal static class ReactionQueries
{
    public static async Task<ReactionResponse?> FirstResponseAsync(
        IQueryable<Reaction> reactions,
        IQueryable<Substance> substances,
        SupportedLocale locale,
        CancellationToken cancellationToken)
    {
        var row = await reactions
            .Select(reaction => new
            {
                reaction.Id,
                reaction.Type,
                reaction.Difficulty,
                reaction.IsReversible,
                reaction.Energetics,
                reaction.Conditions,
                reaction.Visualization,
                reaction.Translations,
                reaction.Tags,
                Participants = reaction.Participants
                    .Join(substances, participant => participant.SubstanceId, substance => substance.Id,
                        (participant, substance) => new ParticipantRow(
                            participant.SubstanceId,
                            participant.Role,
                            participant.Coefficient,
                            participant.State,
                            substance.Formula,
                            substance.Translations))
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

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
            Map(row.Participants, locale),
            row.Energetics.EnthalpyChange?.KilojoulesPerMole,
            row.Energetics.ActivationEnergyKilojoulesPerMole,
            row.Energetics.ReverseActivationEnergyKilojoulesPerMole,
            row.Energetics.IsExothermic,
            row.Conditions.Temperature?.Kelvin,
            row.Conditions.Pressure?.Kilopascals,
            row.Conditions.Catalyst,
            row.Visualization.PresetKey,
            row.Visualization.DurationMilliseconds,
            row.Tags);
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
                reaction.Translations,
                reaction.Tags,
                Participants = reaction.Participants
                    .Join(substances, participant => participant.SubstanceId, substance => substance.Id,
                        (participant, substance) => new ParticipantRow(
                            participant.SubstanceId,
                            participant.Role,
                            participant.Coefficient,
                            participant.State,
                            substance.Formula,
                            substance.Translations))
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return rows.ConvertAll(row => new ReactionSummaryResponse(
            row.Id.Value,
            row.Translations.Resolve(locale).Name,
            row.Type,
            row.Difficulty.Value,
            row.IsReversible,
            row.Energetics.EnthalpyChange?.KilojoulesPerMole,
            row.Energetics.IsExothermic,
            row.Tags,
            Map(row.Participants, locale)));
    }

    private static List<ReactionparticipantResponse> Map(
        List<ParticipantRow> rows, SupportedLocale locale) =>
        rows.ConvertAll(row => new ReactionparticipantResponse(
            row.SubstanceId.Value,
            row.Formula.Value,
            row.Translations.Resolve(locale).Name,
            row.Role,
            row.Coefficient,
            row.State));

    private sealed record ParticipantRow(
        SubstanceId SubstanceId,
        ParticipantRole Role,
        int Coefficient,
        MatterState? State,
        ChemicalFormula Formula,
        Translations<SubstanceContent> Translations);
}
