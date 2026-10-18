using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using ReactionLab.Catalog.Domain.Enums;
using ReactionLab.Catalog.Domain.Substances;
using ReactionLab.Catalog.Infrastructure.Persistence.Documents;
using ReactionLab.Catalog.Infrastructure.Persistence.Interceptors;
using ReactionLab.Catalog.Infrastructure.Persistence.Seeding.Builds;
using ReactionLab.SharedKernel;

namespace ReactionLab.Catalog.Infrastructure.Persistence.Seeding;

internal sealed class BulkCatalogLoader(
    NpgsqlConnection connection,
    NpgsqlTransaction transaction,
    DateTimeOffset now,
    ILogger logger)
{
    private const int CommandTimeoutSeconds = 3600;

    private const decimal RetirementLimit = 0.05m;

    private const string StagingTables = """
        CREATE TEMP TABLE stage_substances (
            id uuid NOT NULL,
            natural_key text NOT NULL,
            formula varchar(100) NOT NULL,
            kind varchar(20) NOT NULL,
            is_organic boolean NOT NULL,
            state_at_room_temperature varchar(20) NOT NULL,
            weight numeric(12,5),
            structure jsonb,
            category varchar(100),
            translations jsonb NOT NULL,
            search_text text NOT NULL,
            element_symbols text[] NOT NULL,
            structure_key varchar(32),
            weaker_keys text[] NOT NULL
        ) ON COMMIT DROP;

        CREATE TEMP TABLE stage_reactions (
            id uuid NOT NULL,
            natural_key text NOT NULL,
            type varchar(40) NOT NULL,
            difficulty integer NOT NULL,
            is_reversible boolean NOT NULL,
            enthalpy_kj_per_mol numeric(12,4),
            activation_energy_kj_per_mol numeric(12,4),
            temperature_k numeric(12,4),
            pressure_kpa numeric(12,4),
            catalyst varchar(200),
            effect_preset_key varchar(50),
            animation_duration_ms integer,
            rule varchar(60) NOT NULL,
            confidence numeric(4,3) NOT NULL,
            tags text[] NOT NULL,
            translations jsonb NOT NULL,
            search_text text NOT NULL,
            reactant_signature uuid[] NOT NULL,
            participant_substances uuid[] NOT NULL,
            participant_roles text[] NOT NULL,
            participant_coefficients integer[] NOT NULL,
            participant_states text[] NOT NULL
        ) ON COMMIT DROP;

        CREATE TEMP TABLE stage_participants (
            id uuid NOT NULL,
            reaction_id uuid NOT NULL,
            substance_id uuid NOT NULL,
            role varchar(20) NOT NULL,
            coefficient integer NOT NULL,
            state varchar(20)
        ) ON COMMIT DROP;
        """;

    private const string IndexStagedSubstances = """
        CREATE INDEX ON stage_substances (natural_key);
        ANALYZE stage_substances;
        """;

    private const string RekeySubstances = """
        WITH candidates AS (
            SELECT DISTINCT ON (st.natural_key) s.id, st.natural_key
            FROM stage_substances st
            CROSS JOIN LATERAL unnest(st.weaker_keys) WITH ORDINALITY AS weaker(natural_key, tier)
            JOIN substances s ON s.natural_key = weaker.natural_key
            WHERE NOT EXISTS (SELECT 1 FROM substances taken WHERE taken.natural_key = st.natural_key)
                AND NOT EXISTS (SELECT 1 FROM stage_substances kept WHERE kept.natural_key = s.natural_key)
            ORDER BY st.natural_key, weaker.tier
        )
        UPDATE substances AS s
        SET natural_key = candidates.natural_key, updated_at = @now
        FROM candidates
        WHERE s.id = candidates.id;
        """;

    private const string MergeSubstances = """
        INSERT INTO substances AS s (
            id, natural_key, formula, kind, is_organic, state_at_room_temperature, weight, structure, category,
            translations, search_text, element_symbols, structure_key, created_at, updated_at, retired_at)
        SELECT id, natural_key, formula, kind, is_organic, state_at_room_temperature, weight, structure, category,
            translations, search_text, element_symbols, structure_key, @now, @now, NULL
        FROM stage_substances
        ON CONFLICT (natural_key) DO UPDATE SET
            formula = excluded.formula,
            kind = excluded.kind,
            is_organic = excluded.is_organic,
            state_at_room_temperature = excluded.state_at_room_temperature,
            weight = excluded.weight,
            structure = excluded.structure,
            category = excluded.category,
            translations = excluded.translations,
            search_text = excluded.search_text,
            element_symbols = excluded.element_symbols,
            structure_key = excluded.structure_key,
            updated_at = excluded.updated_at,
            retired_at = NULL
        WHERE (s.formula, s.kind, s.is_organic, s.state_at_room_temperature, s.weight, s.structure, s.category,
                s.translations, s.search_text, s.element_symbols, s.structure_key, s.retired_at IS NULL)
            IS DISTINCT FROM (excluded.formula, excluded.kind, excluded.is_organic, excluded.state_at_room_temperature,
                excluded.weight, excluded.structure, excluded.category, excluded.translations, excluded.search_text,
                excluded.element_symbols, excluded.structure_key, true);
        """;

    private const string SubstanceIds = """
        SELECT st.natural_key, s.id
        FROM stage_substances st
        JOIN substances s ON s.natural_key = st.natural_key;
        """;

    private const string IndexStagedReactions = """
        INSERT INTO stage_participants (id, reaction_id, substance_id, role, coefficient, state)
        SELECT uuidv7(), r.id, p.substance_id, p.role, p.coefficient, p.state
        FROM stage_reactions r,
            unnest(r.participant_substances, r.participant_roles, r.participant_coefficients, r.participant_states)
                AS p(substance_id, role, coefficient, state);

        CREATE INDEX ON stage_reactions (natural_key);
        CREATE INDEX ON stage_participants (reaction_id);
        ANALYZE stage_reactions;
        ANALYZE stage_participants;
        """;

    private const string MergeReactions = """
        INSERT INTO reactions AS r (
            id, natural_key, type, difficulty, is_reversible, enthalpy_kj_per_mol, activation_energy_kj_per_mol,
            temperature_k, pressure_kpa, catalyst, effect_preset_key, animation_duration_ms, rule, confidence, tags,
            translations, search_text, reactant_signature, created_at, updated_at, retired_at)
        SELECT id, natural_key, type, difficulty, is_reversible, enthalpy_kj_per_mol, activation_energy_kj_per_mol,
            temperature_k, pressure_kpa, catalyst, effect_preset_key, animation_duration_ms, rule, confidence, tags,
            translations, search_text, reactant_signature, @now, @now, NULL
        FROM stage_reactions
        ON CONFLICT (natural_key) DO UPDATE SET
            type = excluded.type,
            difficulty = excluded.difficulty,
            is_reversible = excluded.is_reversible,
            enthalpy_kj_per_mol = excluded.enthalpy_kj_per_mol,
            activation_energy_kj_per_mol = excluded.activation_energy_kj_per_mol,
            temperature_k = excluded.temperature_k,
            pressure_kpa = excluded.pressure_kpa,
            catalyst = excluded.catalyst,
            effect_preset_key = excluded.effect_preset_key,
            animation_duration_ms = excluded.animation_duration_ms,
            rule = excluded.rule,
            confidence = excluded.confidence,
            tags = excluded.tags,
            translations = excluded.translations,
            search_text = excluded.search_text,
            reactant_signature = excluded.reactant_signature,
            updated_at = excluded.updated_at,
            retired_at = NULL
        WHERE (r.type, r.difficulty, r.is_reversible, r.enthalpy_kj_per_mol, r.activation_energy_kj_per_mol,
                r.temperature_k, r.pressure_kpa, r.catalyst, r.effect_preset_key, r.animation_duration_ms, r.rule,
                r.confidence, r.tags, r.translations, r.search_text, r.reactant_signature, r.retired_at IS NULL)
            IS DISTINCT FROM (excluded.type, excluded.difficulty, excluded.is_reversible, excluded.enthalpy_kj_per_mol,
                excluded.activation_energy_kj_per_mol, excluded.temperature_k, excluded.pressure_kpa, excluded.catalyst,
                excluded.effect_preset_key, excluded.animation_duration_ms, excluded.rule, excluded.confidence,
                excluded.tags, excluded.translations, excluded.search_text, excluded.reactant_signature, true);
        """;

    private const string MergeParticipants = """
        UPDATE stage_participants p
        SET reaction_id = r.id
        FROM stage_reactions st
        JOIN reactions r ON r.natural_key = st.natural_key
        WHERE p.reaction_id = st.id AND r.id <> st.id;

        INSERT INTO reaction_participants (id, reaction_id, substance_id, role, coefficient, state)
        SELECT p.id, p.reaction_id, p.substance_id, p.role, p.coefficient, p.state
        FROM stage_participants p
        WHERE NOT EXISTS (
            SELECT 1 FROM reaction_participants e
            WHERE e.reaction_id = p.reaction_id AND e.substance_id = p.substance_id AND e.role = p.role);

        UPDATE reaction_participants e
        SET state = p.state
        FROM stage_participants p
        WHERE e.reaction_id = p.reaction_id AND e.substance_id = p.substance_id AND e.role = p.role
            AND e.state IS DISTINCT FROM p.state;
        """;

    private const int Samples = 5;

    private readonly Dictionary<string, List<(string NaturalKey, string Name)>> _substancesByFormula = new(StringComparer.Ordinal);

    private readonly Dictionary<string, (int Count, List<string> Samples)> _skipsByReason = new(StringComparer.Ordinal);

    public async Task<ImportCounts> LoadAsync(
        ICatalogBuild build,
        CatalogRecordMapper mapper,
        bool allowMassRetirement,
        CancellationToken cancellationToken)
    {
        var watch = Stopwatch.StartNew();

        await ExecuteAsync(StagingTables, cancellationToken);

        var stagedSubstances = await StageSubstancesAsync(build, mapper, cancellationToken);
        Phase(watch, "staged substances");

        await ExecuteAsync(IndexStagedSubstances, cancellationToken);

        var rekeyedSubstances = await ExecuteAsync(RekeySubstances, cancellationToken);
        var writtenSubstances = await ExecuteAsync(MergeSubstances, cancellationToken);
        var retiredSubstances = await RetireMissingAsync("substances", "stage_substances", allowMassRetirement, cancellationToken);
        Phase(watch, "merged substances");

        var substanceIdsByKey = await SubstanceIdsAsync(cancellationToken);
        var (stagedReactions, duplicates) = await StageReactionsAsync(build, mapper, substanceIdsByKey, cancellationToken);
        Phase(watch, "staged reactions");

        await ExecuteAsync(IndexStagedReactions, cancellationToken);

        var writtenReactions = await ExecuteAsync(MergeReactions, cancellationToken);
        Phase(watch, "merged reactions");

        await ExecuteAsync(MergeParticipants, cancellationToken);

        var retiredReactions = await RetireMissingAsync("reactions", "stage_reactions", allowMassRetirement, cancellationToken);
        Phase(watch, "merged participants and retired");

        foreach (var (reason, (count, samples)) in _skipsByReason)
        {
            logger.LogWarning("Skipped {Count} {Reason}, such as {Samples}.", count, reason, string.Join("; ", samples));
        }

        return new ImportCounts(
            stagedSubstances, rekeyedSubstances, writtenSubstances, retiredSubstances,
            stagedReactions, writtenReactions, retiredReactions, duplicates);
    }

    private async Task<int> StageSubstancesAsync(
        ICatalogBuild build,
        CatalogRecordMapper mapper,
        CancellationToken cancellationToken)
    {
        var naturalKeys = new HashSet<string>(StringComparer.Ordinal);
        var staged = 0;

        await using var importer = await connection.BeginBinaryImportAsync(
            """
            COPY stage_substances (id, natural_key, formula, kind, is_organic, state_at_room_temperature, weight,
                structure, category, translations, search_text, element_symbols, structure_key, weaker_keys) FROM STDIN (FORMAT BINARY)
            """,
            cancellationToken);

        await foreach (var record in build.SubstancesAsync(cancellationToken))
        {
            if (mapper.MapSubstance(record) is not { } substance)
            {
                continue;
            }

            var structureKey = StructureKeys.Of(substance.Structure);
            var name = substance.Content(SupportedLocale.Default).Name;
            var naturalKey = NaturalKeys.ForSubstance(record.PubChemCid, structureKey, substance.Formula.Value, name);

            if (!naturalKeys.Add(naturalKey))
            {
                Skip("substances that duplicate another one's natural key", $"{naturalKey} ({name})");

                continue;
            }

            Remember(substance.Formula.Value, naturalKey, name);

            await importer.StartRowAsync(cancellationToken);
            await importer.WriteAsync(Guid.CreateVersion7(), NpgsqlDbType.Uuid, cancellationToken);
            await importer.WriteAsync(naturalKey, NpgsqlDbType.Text, cancellationToken);
            await importer.WriteAsync(substance.Formula.Value, NpgsqlDbType.Varchar, cancellationToken);
            await importer.WriteAsync(substance.Kind.ToString(), NpgsqlDbType.Varchar, cancellationToken);
            await importer.WriteAsync(substance.IsOrganic, NpgsqlDbType.Boolean, cancellationToken);
            await importer.WriteAsync(substance.StateAtRoomTemperature.ToString(), NpgsqlDbType.Varchar, cancellationToken);
            await WriteNullableAsync(importer, substance.Weight?.GramsPerMole, NpgsqlDbType.Numeric, cancellationToken);
            await WriteNullableAsync(
                importer,
                substance.Structure is { } structure ? MolecularStructureDocument.Serialize(structure) : null,
                NpgsqlDbType.Jsonb,
                cancellationToken);
            await WriteNullableAsync(importer, substance.Category, NpgsqlDbType.Varchar, cancellationToken);
            await importer.WriteAsync(SubstanceContentDocument.Serialize(substance.Translations), NpgsqlDbType.Jsonb, cancellationToken);
            await importer.WriteAsync(SearchProjectionInterceptor.SearchTextFor(substance), NpgsqlDbType.Text, cancellationToken);
            await importer.WriteAsync(SearchProjectionInterceptor.ElementSymbolFor(substance), NpgsqlDbType.Array | NpgsqlDbType.Text, cancellationToken);
            await WriteNullableAsync(importer, structureKey, NpgsqlDbType.Varchar, cancellationToken);
            await importer.WriteAsync(
                NaturalKeys.WeakerKeysForSubstance(record.PubChemCid, structureKey, substance.Formula.Value, name),
                NpgsqlDbType.Array | NpgsqlDbType.Text,
                cancellationToken);

            staged++;
        }

        await importer.CompleteAsync(cancellationToken);

        return staged;
    }

    private async Task<(int Staged, int Duplicates)> StageReactionsAsync(
        ICatalogBuild build,
        CatalogRecordMapper mapper,
        Dictionary<string, Guid> substanceIdsByKey,
        CancellationToken cancellationToken)
    {
        var seen = new HashSet<UInt128>();
        var staged = 0;
        var duplicates = 0;

        await using var importer = await connection.BeginBinaryImportAsync(
            """
            COPY stage_reactions (id, natural_key, type, difficulty, is_reversible, enthalpy_kj_per_mol,
                activation_energy_kj_per_mol, temperature_k, pressure_kpa, catalyst, effect_preset_key,
                animation_duration_ms, rule, confidence, tags, translations, search_text, reactant_signature,
                participant_substances, participant_roles, participant_coefficients, participant_states)
            FROM STDIN (FORMAT BINARY)
            """,
            cancellationToken);

        await foreach (var record in build.ReactionsAsync(cancellationToken))
        {
            if (mapper.MapReaction(record) is not { } seed
                || Resolve(seed, substanceIdsByKey) is not { } sides)
            {
                continue;
            }

            var naturalKey = NaturalKeys.ForReaction(sides, seed.Provenance.Rule);

            if (!seen.Add(Fingerprint(naturalKey)))
            {
                duplicates++;

                continue;
            }

            var reactionId = Guid.CreateVersion7();

            await WriteReactionAsync(importer, reactionId, naturalKey, seed, sides, cancellationToken);

            staged++;
        }

        await importer.CompleteAsync(cancellationToken);

        if (duplicates > 0)
        {
            logger.LogWarning("Skipped {Count} duplicate reaction(s) with an equation and rule already in the build.", duplicates);
        }

        return (staged, duplicates);
    }

    private static async Task WriteReactionAsync(
        NpgsqlBinaryImporter importer,
        Guid reactionId,
        string naturalKey,
        ReactionSeed seed,
        IReadOnlyList<NaturalKeys.ReactionSide> sides,
        CancellationToken cancellationToken)
    {
        var energetics = seed.Energetics;
        var conditions = seed.Conditions;
        var visualization = seed.Visualization;
        var reactantSignature = sides
            .Where(side => side.Role == ParticipantRole.Reactant)
            .Select(side => side.SubstanceId)
            .Distinct()
            .Order()
            .ToArray();

        await importer.StartRowAsync(cancellationToken);
        await importer.WriteAsync(reactionId, NpgsqlDbType.Uuid, cancellationToken);
        await importer.WriteAsync(naturalKey, NpgsqlDbType.Text, cancellationToken);
        await importer.WriteAsync(seed.Type.ToString(), NpgsqlDbType.Varchar, cancellationToken);
        await importer.WriteAsync(seed.Difficulty.Value, NpgsqlDbType.Integer, cancellationToken);
        await importer.WriteAsync(seed.IsReversible, NpgsqlDbType.Boolean, cancellationToken);
        await WriteNullableAsync(importer, energetics?.EnthalpyChange?.KilojoulesPerMole, NpgsqlDbType.Numeric, cancellationToken);
        await WriteNullableAsync(importer, energetics?.ActivationEnergyKilojoulesPerMole, NpgsqlDbType.Numeric, cancellationToken);
        await WriteNullableAsync(importer, conditions?.Temperature?.Kelvin, NpgsqlDbType.Numeric, cancellationToken);
        await WriteNullableAsync(importer, conditions?.Pressure?.Kilopascals, NpgsqlDbType.Numeric, cancellationToken);
        await WriteNullableAsync(importer, conditions?.Catalyst, NpgsqlDbType.Varchar, cancellationToken);
        await WriteNullableAsync(importer, visualization?.PresetKey, NpgsqlDbType.Varchar, cancellationToken);
        await WriteNullableAsync(importer, visualization?.DurationMilliseconds, NpgsqlDbType.Integer, cancellationToken);
        await importer.WriteAsync(seed.Provenance.Rule, NpgsqlDbType.Varchar, cancellationToken);
        await importer.WriteAsync(seed.Provenance.Confidence, NpgsqlDbType.Numeric, cancellationToken);
        await importer.WriteAsync(CleanTags(seed.Tags), NpgsqlDbType.Array | NpgsqlDbType.Text, cancellationToken);
        await importer.WriteAsync(ReactionContentDocument.Serialize(seed.Translations), NpgsqlDbType.Jsonb, cancellationToken);
        await importer.WriteAsync(SearchProjectionInterceptor.SearchTextFor(seed.Translations), NpgsqlDbType.Text, cancellationToken);
        await importer.WriteAsync(reactantSignature, NpgsqlDbType.Array | NpgsqlDbType.Uuid, cancellationToken);
        await importer.WriteAsync(sides.Select(side => side.SubstanceId).ToArray(), NpgsqlDbType.Array | NpgsqlDbType.Uuid, cancellationToken);
        await importer.WriteAsync(sides.Select(side => side.Role.ToString()).ToArray(), NpgsqlDbType.Array | NpgsqlDbType.Text, cancellationToken);
        await importer.WriteAsync(sides.Select(side => side.Coefficient).ToArray(), NpgsqlDbType.Array | NpgsqlDbType.Integer, cancellationToken);
        await importer.WriteAsync(seed.Participants.Select(participant => participant.State?.ToString()).ToArray(), NpgsqlDbType.Array | NpgsqlDbType.Text, cancellationToken);
    }

    private List<NaturalKeys.ReactionSide>? Resolve(ReactionSeed seed, Dictionary<string, Guid> substanceIdsByKey)
    {
        var sides = new List<NaturalKeys.ReactionSide>(seed.Participants.Count);

        foreach (var participant in seed.Participants)
        {
            if (participant.Coefficient < 1
                || !_substancesByFormula.TryGetValue(participant.Formula, out var candidates))
            {
                Skip("reactions with a participant missing from the build", $"{seed.Key}: {participant.Formula}");

                return null;
            }

            var match = candidates.Count == 1
                ? candidates[0].NaturalKey
                : candidates.FirstOrDefault(candidate =>
                    string.Equals(candidate.Name, participant.Substance, StringComparison.OrdinalIgnoreCase)).NaturalKey;

            if (match is null || !substanceIdsByKey.TryGetValue(match, out var substanceId))
            {
                Skip("reactions with a participant no name resolves", $"{seed.Key}: {participant.Formula} \"{participant.Substance}\"");

                return null;
            }

            sides.Add(new NaturalKeys.ReactionSide(substanceId, participant.Role, participant.Coefficient));
        }

        var reactants = sides.Where(side => side.Role == ParticipantRole.Reactant).ToList();
        var products = sides.Where(side => side.Role == ParticipantRole.Product).ToList();

        if (reactants.Count == 0 || products.Count == 0
            || reactants.DistinctBy(side => side.SubstanceId).Count() != reactants.Count
            || products.DistinctBy(side => side.SubstanceId).Count() != products.Count)
        {
            Skip("reactions without distinct reactants and products", seed.Key);

            return null;
        }

        return sides;
    }

    private void Phase(Stopwatch watch, string phase)
    {
        logger.LogInformation("Import: {Phase} in {Seconds:F1} s.", phase, watch.Elapsed.TotalSeconds);
        watch.Restart();
    }

    private void Skip(string reason, string sample)
    {
        var (count, samples) = _skipsByReason.TryGetValue(reason, out var known) ? known : (0, new List<string>());

        if (samples.Count < Samples)
        {
            samples.Add(sample);
        }

        _skipsByReason[reason] = (count + 1, samples);
    }

    private void Remember(string formula, string naturalKey, string name)
    {
        if (!_substancesByFormula.TryGetValue(formula, out var candidates))
        {
            candidates = [];
            _substancesByFormula[formula] = candidates;
        }

        candidates.Add((naturalKey, name));
    }

    private async Task<Dictionary<string, Guid>> SubstanceIdsAsync(CancellationToken cancellationToken)
    {
        var idsByKey = new Dictionary<string, Guid>(StringComparer.Ordinal);

        await using var command = Command(SubstanceIds);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            idsByKey[reader.GetString(0)] = reader.GetGuid(1);
        }

        return idsByKey;
    }

    private async Task<int> RetireMissingAsync(
        string table,
        string stage,
        bool allowMassRetirement,
        CancellationToken cancellationToken)
    {
        var missing = $"""
            {table}.retired_at IS NULL
            AND NOT EXISTS (SELECT 1 FROM {stage} st WHERE st.natural_key = {table}.natural_key)
            """;

        await using var count = Command($"""
            SELECT count(*) FILTER (WHERE {missing}), count(*) FILTER (WHERE {table}.retired_at IS NULL)
            FROM {table}
            """);
        await using var reader = await count.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);

        var toRetire = reader.GetInt64(0);
        var active = reader.GetInt64(1);

        await reader.CloseAsync();

        if (toRetire == 0)
        {
            return 0;
        }

        if (!allowMassRetirement && toRetire > active * RetirementLimit)
        {
            throw new InvalidOperationException(
                $"The import would retire {toRetire} of {active} active {table}, more than {RetirementLimit:P0}. " +
                "Check the build, or set CatalogImport:AllowMassRetirement.");
        }

        await using var retire = Command($"UPDATE {table} SET retired_at = @now, updated_at = @now WHERE {missing}");

        return await retire.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<int> ExecuteAsync(string sql, CancellationToken cancellationToken)
    {
        await using var command = Command(sql);

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private NpgsqlCommand Command(string sql)
    {
        var command = new NpgsqlCommand(sql, connection, transaction) { CommandTimeout = CommandTimeoutSeconds };

        command.Parameters.AddWithValue("now", now);

        return command;
    }

    private static string[] CleanTags(IEnumerable<string> tags) =>
        [.. tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).Select(tag => tag.Trim())];

    private static UInt128 Fingerprint(string naturalKey)
    {
        Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];

        SHA256.HashData(Encoding.UTF8.GetBytes(naturalKey), hash);

        return new UInt128(BitConverter.ToUInt64(hash[..8]), BitConverter.ToUInt64(hash[8..16]));
    }

    private static async Task WriteNullableAsync<T>(
        NpgsqlBinaryImporter importer,
        T? value,
        NpgsqlDbType type,
        CancellationToken cancellationToken)
    {
        if (value is null)
        {
            await importer.WriteNullAsync(cancellationToken);
        }
        else
        {
            await importer.WriteAsync(value, type, cancellationToken);
        }
    }
}
