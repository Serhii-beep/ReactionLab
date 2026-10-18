using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using ReactionLab.Catalog.Application.Features.Releases.Search;
using ReactionLab.Catalog.Domain.Reactions;
using ReactionLab.Catalog.Domain.Substances;
using ReactionLab.Catalog.Infrastructure.Persistence.Documents;
using ReactionLab.Catalog.Infrastructure.Persistence.Releases;
using ReactionLab.SharedKernel;

namespace ReactionLab.Catalog.Infrastructure.Releases;

internal sealed class ReleaseSnapshotBuilder(ILogger<ReleaseSnapshotBuilder> logger)
{
    public const int PrefixTopSize = 30;

    private const int CommandTimeoutSeconds = 3600;

    private const int NameBatchSize = 20_000;

    private const int BuildParallelism = 4;

    private const int CommonNamesPerSubstance = 10;

    public static string PartitionOf(string table, Guid releaseId) => $"{table}_{releaseId:N}";

    public async Task BuildAsync(
        Guid releaseId,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var snapshot = new SnapshotConnection(releaseId, connection, transaction);

        await snapshot.ExecuteAsync($"SET LOCAL max_parallel_workers_per_gather = {BuildParallelism}", cancellationToken);

        foreach (var table in ReleaseReadContext.Tables)
        {
            await snapshot.ExecuteAsync(
                $"CREATE TABLE {PartitionOf(table, releaseId)} (LIKE {table} INCLUDING DEFAULTS)",
                cancellationToken);
        }

        await snapshot.ExecuteAsync(SubstancesSql(releaseId), cancellationToken);

        var names = await WriteNamesAsync(snapshot, cancellationToken);

        await snapshot.ExecuteAsync(ElementsSql(releaseId), cancellationToken);
        await snapshot.ExecuteAsync(ReactionsSql(releaseId), cancellationToken);
        await snapshot.ExecuteAsync(ParticipantsSql(releaseId), cancellationToken);

        foreach (var table in ReleaseReadContext.Tables)
        {
            var partition = PartitionOf(table, releaseId);

            await snapshot.ExecuteAsync(
                $"ALTER TABLE {table} ATTACH PARTITION {partition} FOR VALUES IN ('{releaseId}')",
                cancellationToken);
            await snapshot.ExecuteAsync($"ANALYZE {partition}", cancellationToken);
        }

        logger.LogInformation("Built the snapshot of release {Release}: {Names} searchable name(s).", releaseId, names);
    }

    public static async Task DropAsync(Guid releaseId, NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        foreach (var table in ReleaseReadContext.Tables)
        {
            var partition = PartitionOf(table, releaseId);

            if (await DetachPendingAsync(connection, table, partition, cancellationToken) is { } detachPending)
            {
                await ExecuteOutsideTransactionAsync(
                    connection,
                    $"ALTER TABLE {table} DETACH PARTITION {partition} {(detachPending ? "FINALIZE" : "CONCURRENTLY")}",
                    cancellationToken);
            }

            await ExecuteOutsideTransactionAsync(connection, $"DROP TABLE IF EXISTS {partition}", cancellationToken);
        }
    }

    private static async Task<bool?> DetachPendingAsync(
        NpgsqlConnection connection,
        string table,
        string partition,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT i.inhdetachpending
            FROM pg_inherits i
            JOIN pg_class child ON child.oid = i.inhrelid
            JOIN pg_class parent ON parent.oid = i.inhparent
            WHERE child.relname = @partition AND parent.relname = @table
            """,
            connection);

        command.Parameters.AddWithValue("partition", partition);
        command.Parameters.AddWithValue("table", table);

        return await command.ExecuteScalarAsync(cancellationToken) is bool pending ? pending : null;
    }

    private static async Task ExecuteOutsideTransactionAsync(NpgsqlConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection) { CommandTimeout = CommandTimeoutSeconds };

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string SubstancesSql(Guid releaseId) => $"""
        INSERT INTO {PartitionOf(ReleaseReadContext.Substances, releaseId)} (
            release_id, id, formula, kind, is_organic, state_at_room_temperature, weight, structure, category,
            translations, retired_at, prominence, structure_key)
        SELECT @release, s.id, s.formula, s.kind, s.is_organic, s.state_at_room_temperature, s.weight, s.structure,
            s.category, s.translations, s.retired_at, coalesce(taking_part.reactions, 0)::integer, s.structure_key
        FROM substances s
        LEFT JOIN (
            SELECT p.substance_id, count(DISTINCT p.reaction_id) AS reactions
            FROM reaction_participants p
            JOIN reactions r ON r.id = p.reaction_id AND r.retired_at IS NULL
            GROUP BY p.substance_id
        ) taking_part ON taking_part.substance_id = s.id
        """;

    private static string ElementsSql(Guid releaseId) => $"""
        INSERT INTO {PartitionOf(ReleaseReadContext.SubstanceElements, releaseId)} (release_id, element, prominence, substance_id)
        SELECT @release, symbol, snapshot.prominence, snapshot.id
        FROM {PartitionOf(ReleaseReadContext.Substances, releaseId)} snapshot
        JOIN substances s ON s.id = snapshot.id
        CROSS JOIN LATERAL unnest(s.element_symbols) AS symbol
        WHERE snapshot.retired_at IS NULL
        """;

    private static string ReactionsSql(Guid releaseId) => $"""
        INSERT INTO {PartitionOf(ReleaseReadContext.Reactions, releaseId)} (
            release_id, id, type, difficulty, is_reversible, retired_at, enthalpy_kj_per_mol,
            activation_energy_kj_per_mol, temperature_k, pressure_kpa, catalyst, effect_preset_key,
            animation_duration_ms, rule, confidence, tags, translations, reactant_ids, rank)
        SELECT @release, r.id, r.type, r.difficulty, r.is_reversible, r.retired_at, r.enthalpy_kj_per_mol,
            r.activation_energy_kj_per_mol, r.temperature_k, r.pressure_kpa, r.catalyst, r.effect_preset_key,
            r.animation_duration_ms, r.rule, r.confidence, r.tags, r.translations, r.reactant_signature,
            (row_number() OVER (
                ORDER BY (r.rule = '{ReactionProvenance.CuratedRule}') DESC, coalesce(reactants.prominence, 0) DESC, r.id) - 1)::integer
        FROM reactions r
        LEFT JOIN (
            SELECT p.reaction_id, sum(snapshot.prominence) AS prominence
            FROM reaction_participants p
            JOIN {PartitionOf(ReleaseReadContext.Substances, releaseId)} snapshot ON snapshot.id = p.substance_id
            WHERE p.role = 'Reactant'
            GROUP BY p.reaction_id
        ) reactants ON reactants.reaction_id = r.id
        """;

    private static string ParticipantsSql(Guid releaseId) => $"""
        INSERT INTO {PartitionOf(ReleaseReadContext.Participants, releaseId)} (
            release_id, id, reaction_id, substance_id, role, coefficient, state)
        SELECT @release, p.id, p.reaction_id, p.substance_id, p.role, p.coefficient, p.state
        FROM reaction_participants p
        """;

    private static async Task<int> WriteNamesAsync(SnapshotConnection snapshot, CancellationToken cancellationToken)
    {
        var tops = new PrefixTops();
        var written = 0;
        Guid? after = null;

        while (true)
        {
            var batch = new List<(string Locale, Guid SubstanceId, string Key, int Prominence)>();
            var rows = 0;

            await using (var command = snapshot.Command($"""
                SELECT id, formula, translations::text, prominence
                FROM {PartitionOf(ReleaseReadContext.Substances, snapshot.ReleaseId)}
                WHERE retired_at IS NULL AND (@after::uuid IS NULL OR id > @after::uuid)
                ORDER BY id
                LIMIT {NameBatchSize}
                """))
            {
                command.Parameters.AddWithValue("after", NpgsqlDbType.Uuid, (object?)after ?? DBNull.Value);

                await using var reader = await command.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    var substanceId = reader.GetGuid(0);
                    var formula = ChemicalFormula.Create(reader.GetString(1));
                    var translations = SubstanceContentDocument.Deserialize(reader.GetString(2));
                    var prominence = reader.GetInt32(3);
                    var formulas = formula.IsSuccess ? new[] { formula.Value.Value, formula.Value.Hill } : [];

                    foreach (var locale in SupportedLocale.All)
                    {
                        foreach (var key in KeysFor(translations, locale, formulas))
                        {
                            batch.Add((locale.Code, substanceId, key, prominence));
                            tops.Add(locale.Code, key, substanceId, prominence);
                        }
                    }

                    after = substanceId;
                    rows++;
                }
            }

            if (rows == 0)
            {
                break;
            }

            await CopyNamesAsync(snapshot, batch, cancellationToken);
            written += batch.Count;
        }

        await CopyTopsAsync(snapshot, tops, cancellationToken);
        await CopyWordsAsync(snapshot, tops, cancellationToken);

        return written;
    }

    private static async Task CopyNamesAsync(
        SnapshotConnection snapshot,
        List<(string Locale, Guid SubstanceId, string Key, int Prominence)> names,
        CancellationToken cancellationToken)
    {
        await using var importer = await snapshot.Connection.BeginBinaryImportAsync(
            $"COPY {PartitionOf(ReleaseReadContext.SubstanceNames, snapshot.ReleaseId)} (release_id, locale, substance_id, name_key, prominence) FROM STDIN (FORMAT BINARY)",
            cancellationToken);

        foreach (var (locale, substanceId, key, prominence) in names)
        {
            await importer.StartRowAsync(cancellationToken);
            await importer.WriteAsync(snapshot.ReleaseId, NpgsqlDbType.Uuid, cancellationToken);
            await importer.WriteAsync(locale, NpgsqlDbType.Varchar, cancellationToken);
            await importer.WriteAsync(substanceId, NpgsqlDbType.Uuid, cancellationToken);
            await importer.WriteAsync(key, NpgsqlDbType.Text, cancellationToken);
            await importer.WriteAsync(prominence, NpgsqlDbType.Integer, cancellationToken);
        }

        await importer.CompleteAsync(cancellationToken);
    }

    private static async Task CopyTopsAsync(SnapshotConnection snapshot, PrefixTops tops, CancellationToken cancellationToken)
    {
        await using var importer = await snapshot.Connection.BeginBinaryImportAsync(
            $"COPY {PartitionOf(ReleaseReadContext.PrefixTops, snapshot.ReleaseId)} (release_id, locale, prefix, rank, substance_id, prominence) FROM STDIN (FORMAT BINARY)",
            cancellationToken);

        foreach (var (locale, prefix, rank, substanceId, prominence) in tops.Rows())
        {
            await importer.StartRowAsync(cancellationToken);
            await importer.WriteAsync(snapshot.ReleaseId, NpgsqlDbType.Uuid, cancellationToken);
            await importer.WriteAsync(locale, NpgsqlDbType.Varchar, cancellationToken);
            await importer.WriteAsync(prefix, NpgsqlDbType.Text, cancellationToken);
            await importer.WriteAsync(rank, NpgsqlDbType.Smallint, cancellationToken);
            await importer.WriteAsync(substanceId, NpgsqlDbType.Uuid, cancellationToken);
            await importer.WriteAsync(prominence, NpgsqlDbType.Integer, cancellationToken);
        }

        await importer.CompleteAsync(cancellationToken);
    }

    private static async Task CopyWordsAsync(SnapshotConnection snapshot, PrefixTops tops, CancellationToken cancellationToken)
    {
        await using var importer = await snapshot.Connection.BeginBinaryImportAsync(
            $"COPY {PartitionOf(ReleaseReadContext.Words, snapshot.ReleaseId)} (release_id, locale, word, prominence) FROM STDIN (FORMAT BINARY)",
            cancellationToken);

        foreach (var (locale, word, prominence) in tops.Words())
        {
            await importer.StartRowAsync(cancellationToken);
            await importer.WriteAsync(snapshot.ReleaseId, NpgsqlDbType.Uuid, cancellationToken);
            await importer.WriteAsync(locale, NpgsqlDbType.Varchar, cancellationToken);
            await importer.WriteAsync(word, NpgsqlDbType.Text, cancellationToken);
            await importer.WriteAsync(prominence, NpgsqlDbType.Integer, cancellationToken);
        }

        await importer.CompleteAsync(cancellationToken);
    }

    private static IEnumerable<string> KeysFor(
        Translations<SubstanceContent> translations,
        SupportedLocale locale,
        IEnumerable<string> formulas)
    {
        var contents = locale == SupportedLocale.Default
            ? [translations.Resolve(locale)]
            : new[] { translations.Resolve(locale), translations.Resolve(SupportedLocale.Default) };

        return contents
            .SelectMany(content => new[] { content.Name, content.IupacName }.Concat(content.CommonNames.Take(CommonNamesPerSubstance)))
            .Concat(formulas)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => SearchKeys.Normalize(name!))
            .Where(key => key.Length > 0)
            .Distinct(StringComparer.Ordinal);
    }

    private sealed class PrefixTops
    {
        private readonly Dictionary<(string Locale, string Prefix), List<(Guid SubstanceId, int Prominence)>> _topsByBucket = [];

        public void Add(string locale, string key, Guid substanceId, int prominence)
        {
            foreach (var prefix in SearchKeys.Words(key).SelectMany(SearchKeys.Buckets).Distinct(StringComparer.Ordinal))
            {
                if (!_topsByBucket.TryGetValue((locale, prefix), out var bucket))
                {
                    bucket = [];
                    _topsByBucket[(locale, prefix)] = bucket;
                }

                Offer(bucket, substanceId, prominence);
            }
        }

        public IEnumerable<(string Locale, string Prefix, short Rank, Guid SubstanceId, int Prominence)> Rows() =>
            _topsByBucket.SelectMany(entry => entry.Value
                .OrderByDescending(candidate => candidate.Prominence)
                .ThenBy(candidate => candidate.SubstanceId)
                .Select((candidate, rank) =>
                    (entry.Key.Locale, entry.Key.Prefix, (short)rank, candidate.SubstanceId, candidate.Prominence)));

        public IEnumerable<(string Locale, string Word, int Prominence)> Words() =>
            _topsByBucket
                .Where(entry => SearchKeys.LengthOf(entry.Key.Prefix) > SearchKeys.MaximumPrefixLength)
                .Select(entry => (entry.Key.Locale, entry.Key.Prefix, entry.Value.Max(candidate => candidate.Prominence)));

        private static void Offer(List<(Guid SubstanceId, int Prominence)> bucket, Guid substanceId, int prominence)
        {
            if (bucket.Exists(candidate => candidate.SubstanceId == substanceId))
            {
                return;
            }

            if (bucket.Count < PrefixTopSize)
            {
                bucket.Add((substanceId, prominence));

                return;
            }

            var worst = 0;

            for (var index = 1; index < bucket.Count; index++)
            {
                if (Outranks(bucket[worst], bucket[index]))
                {
                    worst = index;
                }
            }

            if (Outranks((substanceId, prominence), bucket[worst]))
            {
                bucket[worst] = (substanceId, prominence);
            }
        }

        private static bool Outranks((Guid SubstanceId, int Prominence) first, (Guid SubstanceId, int Prominence) second) =>
            first.Prominence > second.Prominence
            || (first.Prominence == second.Prominence && first.SubstanceId.CompareTo(second.SubstanceId) < 0);
    }

    private sealed class SnapshotConnection(Guid releaseId, NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        public Guid ReleaseId => releaseId;

        public NpgsqlConnection Connection => connection;

        public async Task ExecuteAsync(string sql, CancellationToken cancellationToken)
        {
            await using var command = Command(sql);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public NpgsqlCommand Command(string sql)
        {
            var command = new NpgsqlCommand(sql, connection, transaction) { CommandTimeout = CommandTimeoutSeconds };

            command.Parameters.AddWithValue("release", releaseId);

            return command;
        }
    }
}
