using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ReactionLab.Domain.Localization;
using ReactionLab.Domain.Reactions;
using ReactionLab.Domain.Substances;

namespace ReactionLab.Infrastructure.Persistence.Seeding;

internal sealed class CatalogSeeder(
    AppDbContext context,
    ICatalogSource source,
    ILogger<CatalogSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var batch = await source.LoadAsync(cancellationToken);

        foreach (var rejection in batch.Rejections)
        {
            logger.LogWarning("Rejected from {Source}: {Rejection}", source.Name, rejection);
        }

        var elements = await SeedElementsAsync(batch, cancellationToken);
        var substances = await SeedSubstancesAsync(batch, cancellationToken);
        var reactions = await SeedReactionsAsync(batch, cancellationToken);

        logger.LogInformation(
            "Seeded {Elements} element(s), {Substances} substance(s), {Reactions} reaction(s) from {Source}.",
            elements, substances, reactions, source.Name);
    }

    private async Task<int> SeedElementsAsync(CatalogBatch batch, CancellationToken cancellationToken)
    {
        var existing = await context.Elements
            .Select(e => e.Symbol.Value)
            .ToListAsync(cancellationToken);

        var known = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = batch.Elements.Where(e => !known.Contains(e.Symbol.Value)).ToList();

        context.Elements.AddRange(missing);
        await context.SaveChangesAsync(cancellationToken);

        return missing.Count;
    }

    private async Task<int> SeedSubstancesAsync(CatalogBatch batch, CancellationToken cancellationToken)
    {
        var known = (await context.Substances.ToListAsync(cancellationToken))
            .Select(GetKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missing = batch.Substances.Where(substance => !known.Contains(GetKey(substance))).ToList();

        context.Substances.AddRange(missing);
        await context.SaveChangesAsync(cancellationToken);

        return missing.Count;
    }

    private async Task<int> SeedReactionsAsync(CatalogBatch batch, CancellationToken cancellationToken)
    {
        var stored = await context.Reactions.ToListAsync(cancellationToken);
        var known = stored
            .Select(reaction => reaction.Content(SupportedLocale.Default).Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var byFormula = (await context.Substances.ToListAsync(cancellationToken))
            .GroupBy(substance => substance.Formula.Value, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);

        var seeded = 0;

        foreach (var seed in batch.Reactions.Where(seed => !known.Contains(seed.Content.Name)))
        {
            if (BuildSpecifications(seed, byFormula) is not { } specifications)
            {
                continue;
            }

            var created = Reaction.Create(seed.Content, seed.Type, specifications, seed.Difficulty, seed.IsReversible);

            if (created.IsFailure)
            {
                logger.LogWarning("Failed to create {Reaction}: {Code} - {Description}", seed.Key, created.Error.Code, created.Error.Description);

                continue;
            }

            var reaction = created.Value;

            if (seed.Energetics is not null)
            {
                reaction.DescribeEnergetics(seed.Energetics);
            }

            if (seed.Conditions is not null)
            {
                reaction.DescribeConditions(seed.Conditions);
            }

            if (seed.Visualization is not null)
            {
                reaction.DescribeVisualization(seed.Visualization);
            }

            reaction.DescribeProvenance(seed.Provenance);
            reaction.ApplyTags(seed.Tags);

            context.Reactions.Add(reaction);
            seeded++;
        }

        await context.SaveChangesAsync(cancellationToken);

        return seeded;
    }

    private List<ParticipantSpecification>? BuildSpecifications(ReactionSeed seed, Dictionary<string, List<Substance>> byFormula)
    {
        var specifications = new List<ParticipantSpecification>(seed.Participants.Count);

        foreach (var participant in seed.Participants)
        {
            if (!byFormula.TryGetValue(participant.Formula, out var candidates))
            {
                logger.LogWarning("Skipping reaction {Reaction}: no substance with formula {Formula} is seeded.", seed.Key, participant.Formula);

                return null;
            }

            var substance = candidates.Count == 1
                ? candidates[0]
                : candidates.FirstOrDefault(candidate => string.Equals(
                    candidate.Content(SupportedLocale.Default).Name,
                    participant.Substance,
                    StringComparison.OrdinalIgnoreCase));

            if (substance is null)
            {
                logger.LogWarning("Skipping reaction {Reaction}: formula {Formula} matches {Count} substances and no name hint resolved it.",
                    seed.Key, participant.Formula, candidates.Count);

                return null;
            }

            specifications.Add(new ParticipantSpecification(substance.Id, substance.Formula, participant.Role, participant.Coefficient, participant.State));
        }

        return specifications;
    }

    private static string GetKey(Substance substance) =>
        $"{substance.Formula.Value}|{substance.Content(SupportedLocale.Default).Name}";
}
