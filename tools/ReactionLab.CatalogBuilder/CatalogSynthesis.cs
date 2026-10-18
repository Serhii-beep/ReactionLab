using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ReactionLab.Catalog.Infrastructure.Persistence.Seeding.Builds;
using ReactionLab.Catalog.Infrastructure.Persistence.Seeding.Catalog;
using ReactionLab.Chemistry.Constitution;
using ReactionLab.Chemistry.Geometry;

namespace ReactionLab.CatalogBuilder;

internal static class CatalogSynthesis
{
    public const string DefaultBuild = "synthetic-100k-1m";

    private const int Seed = 42;
    private const int SubstanceTarget = 100_000;
    private const int ReactionTarget = 1_000_000;
    private const int SubstancesPerShard = 50_000;
    private const int ReactionsPerShard = 100_000;

    private static readonly (string Symbol, int Valence, double Share)[] HeavyAtoms =
    [
        ("C", 4, 0.62), ("O", 2, 0.14), ("N", 3, 0.10), ("Cl", 1, 0.05), ("S", 2, 0.03),
        ("F", 1, 0.03), ("P", 3, 0.02), ("Br", 1, 0.01)
    ];

    private static readonly string[] Substituents =
    [
        "methyl", "ethyl", "propyl", "butyl", "chloro", "bromo", "fluoro", "hydroxy", "amino", "nitro", "oxo",
        "cyano", "phenyl", "benzyl", "acetyl", "formyl", "methoxy", "ethoxy", "sulfanyl", "carboxy"
    ];

    private static readonly string[] Cores =
    [
        "methane", "ethane", "propane", "butane", "pentane", "hexane", "heptane", "octane", "nonane", "decane",
        "benzene", "pyridine", "furan", "thiophene", "pyrrole", "cyclohexane", "naphthalene", "indole", "quinoline"
    ];

    private static readonly string[] Suffixes = ["", "ol", "one", "al", "amine", "amide", "oic acid", "nitrile", "thiol", "ate"];

    private static readonly string[] Multipliers = ["", "di", "tri", "tetra"];

    private static readonly string[] ReactionTypes =
        ["Synthesis", "Decomposition", "SingleReplacement", "DoubleReplacement", "Combustion", "AcidBase", "Oxidation", "Reduction", "Precipitation", "Neutralization"];

    public static async Task RunAsync(string root, string[] args)
    {
        var build = CommandLineOptions.ValueOf(args, "--build") ?? DefaultBuild;
        var output = Path.Combine(root, "data", "builds", build);
        var manifestPath = Path.Combine(output, CatalogBuildManifest.FileName);

        if (!File.Exists(manifestPath))
        {
            await WriteAsync(root, build, output);
        }
        else
        {
            Console.WriteLine($"Build {build} is already written to {output}.");
        }

        if (args.Contains("--upload", StringComparer.Ordinal))
        {
            await UploadAsync(build, output, CommandLineOptions.ValueOf(args, "--connection") ?? Environment.GetEnvironmentVariable("ConnectionStrings__blobs"));
        }
    }

    private static async Task WriteAsync(string root, string build, string output)
    {
        var random = new Random(Seed);
        var curated = Path.Combine(root, "data", "catalog", "v1");
        var elements = await ReadAsync<ElementRecord>(Path.Combine(curated, "elements.jsonl"));
        var substances = await ReadAsync<SubstanceRecord>(Path.Combine(curated, "substances.jsonl"));
        var reactions = await ReadAsync<ReactionRecord>(Path.Combine(curated, "reactions.jsonl"));
        var masses = elements.ToDictionary(element => element.Symbol, element => element.AtomicMass, StringComparer.Ordinal);

        var popularity = reactions
            .SelectMany(reaction => reaction.Participants.Where(participant => participant.Role == "Reactant"))
            .GroupBy(participant => $"{participant.Formula}|{participant.Substance}", StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        var pool = substances
            .Select(substance => (Record: substance, Uses: popularity.GetValueOrDefault($"{substance.Formula}|{substance.Translations["en"].Name}")))
            .OrderByDescending(entry => entry.Uses)
            .Select(entry => entry.Record)
            .ToList();

        var names = pool.Select(substance => substance.Translations["en"].Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var constitutions = pool
            .Where(substance => substance.Structure is not null)
            .Select(substance => KeyOf(substance.Structure!))
            .ToHashSet(StringComparer.Ordinal);

        while (pool.Count < SubstanceTarget)
        {
            var substance = SyntheticSubstance(random, masses, names);

            if (constitutions.Add(KeyOf(substance.Structure!)))
            {
                pool.Add(substance);
            }
            else
            {
                names.Remove(substance.Translations["en"].Name);
            }
        }

        Directory.CreateDirectory(output);

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        var elementShards = await WriteShardsAsync(output, "elements", elements, int.MaxValue, hash);
        var substanceShards = await WriteShardsAsync(output, "substances", pool, SubstancesPerShard, hash);

        var zipf = new ZipfSampler(pool.Count, random);
        var synthetic = Enumerable.Range(0, ReactionTarget - reactions.Count)
            .Select(index => SyntheticReaction(index, random, zipf, pool, substances.Count));
        var reactionShards = await WriteShardsAsync(output, "reactions", reactions.Concat(synthetic), ReactionsPerShard, hash);

        var manifest = new CatalogBuildManifest(
            build,
            Convert.ToHexStringLower(hash.GetHashAndReset()),
            pool.Count,
            ReactionTarget,
            elementShards,
            substanceShards,
            reactionShards);

        await File.WriteAllTextAsync(
            Path.Combine(output, CatalogBuildManifest.FileName),
            JsonSerializer.Serialize(manifest, CatalogJson.Options));

        Console.WriteLine($"Wrote build {build}: {pool.Count} substances, {ReactionTarget} reactions, to {output}.");
    }

    private static SubstanceRecord SyntheticSubstance(Random random, Dictionary<string, decimal> masses, HashSet<string> names)
    {
        var heavyCount = 5 + (int)Math.Min(25, Math.Floor(-Math.Log(1 - random.NextDouble()) * 7));
        var symbols = new List<string>();
        var free = new List<int>();
        var bonds = new List<SubstanceRecord.BondRecord>();

        for (var atom = 0; atom < heavyCount; atom++)
        {
            var (symbol, valence) = PickHeavy(random, atom == 0);
            var attachable = free.Select((count, index) => (count, index)).Where(entry => entry.count > 0).ToList();

            if (atom > 0 && (attachable.Count == 0 || valence == 0))
            {
                break;
            }

            symbols.Add(symbol);
            free.Add(valence);

            if (atom > 0)
            {
                var parent = attachable[random.Next(attachable.Count)].index;

                bonds.Add(new SubstanceRecord.BondRecord(parent, atom, "single"));
                free[parent]--;
                free[atom]--;
            }
        }

        var heavy = symbols.Count;

        for (var atom = 0; atom < heavy; atom++)
        {
            for (var hydrogen = 0; hydrogen < free[atom]; hydrogen++)
            {
                symbols.Add("H");
                bonds.Add(new SubstanceRecord.BondRecord(atom, symbols.Count - 1, "single"));
            }
        }

        var atoms = symbols.Select((symbol, index) => new SubstanceRecord.AtomRecord(
            symbol,
            Math.Round(index * 1.2 + random.NextDouble(), 4),
            Math.Round(random.NextDouble() * 1.5, 4),
            Math.Round(random.NextDouble() * 1.5, 4))).ToList();

        var counts = symbols.GroupBy(symbol => symbol, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var formula = Hill(counts);
        var weight = Math.Round(counts.Sum(pair => masses.GetValueOrDefault(pair.Key) * pair.Value), 3);
        var name = UniqueName(random, names);

        return new SubstanceRecord
        {
            Formula = formula,
            Kind = "Molecular",
            IsOrganic = counts.ContainsKey("C"),
            State = heavy < 5 ? "Gas" : heavy < 12 ? "Liquid" : "Solid",
            MolecularWeight = weight,
            Category = "synthetic",
            Structure = new SubstanceRecord.StructureRecord(atoms, bonds),
            Translations = new Dictionary<string, SubstanceRecord.SubstanceText>(StringComparer.Ordinal)
            {
                ["en"] = new(name, name, null, null, null, null, null)
            }
        };
    }

    private static ReactionRecord SyntheticReaction(int index, Random random, ZipfSampler zipf, List<SubstanceRecord> pool, int curatedCount)
    {
        var size = random.NextDouble() switch { < 0.05 => 1, < 0.85 => 2, < 0.98 => 3, _ => 4 };
        var reactants = new HashSet<int>();

        while (reactants.Count < size)
        {
            reactants.Add(zipf.Next());
        }

        var products = new HashSet<int>();
        var productCount = 1 + random.Next(2);

        while (products.Count < productCount)
        {
            var candidate = curatedCount + random.Next(pool.Count - curatedCount);

            if (!reactants.Contains(candidate))
            {
                products.Add(candidate);
            }
        }

        var reactantNames = reactants.Select(position => pool[position].Translations["en"].Name).ToList();
        var name = reactantNames.Count == 1
            ? $"Decomposition of {reactantNames[0]}"
            : $"Reaction of {string.Join(" with ", reactantNames)}";

        return new ReactionRecord
        {
            Key = string.Create(CultureInfo.InvariantCulture, $"synthetic-{index}"),
            Type = ReactionTypes[random.Next(ReactionTypes.Length)],
            Difficulty = 1 + random.Next(5),
            Rule = "synthetic",
            Confidence = 0.5m,
            Participants =
            [
                .. reactants.Select(position => Participant(pool[position], "Reactant", random)),
                .. products.Select(position => Participant(pool[position], "Product", random))
            ],
            Translations = new Dictionary<string, ReactionRecord.ReactionText>(StringComparer.Ordinal)
            {
                ["en"] = new(name, null, null, null, null)
            }
        };
    }

    private static string KeyOf(SubstanceRecord.StructureRecord structure) =>
        ConstitutionKey.Of(
            [.. structure.Atoms.Select(atom => atom.Symbol)],
            [.. structure.Bonds.Select(bond => new AtomBond(bond.From, bond.To, 1))]);

    private static ReactionRecord.ParticipantRecord Participant(SubstanceRecord substance, string role, Random random) =>
        new(substance.Formula, role, 1 + random.Next(3), null, substance.Translations["en"].Name);

    private static (string Symbol, int Valence) PickHeavy(Random random, bool first)
    {
        if (first)
        {
            return ("C", 4);
        }

        var roll = random.NextDouble();

        foreach (var (symbol, valence, share) in HeavyAtoms)
        {
            if ((roll -= share) < 0)
            {
                return (symbol, valence);
            }
        }

        return ("C", 4);
    }

    private static string UniqueName(Random random, HashSet<string> names)
    {
        while (true)
        {
            var multiplier = Multipliers[random.Next(Multipliers.Length)];
            var substituent = Substituents[random.Next(Substituents.Length)];
            var core = Cores[random.Next(Cores.Length)];
            var suffix = Suffixes[random.Next(Suffixes.Length)];
            var locant = 1 + random.Next(9);
            var stem = suffix.Length == 0 ? core : core.TrimEnd('e') + suffix;
            var name = string.Create(CultureInfo.InvariantCulture, $"{locant}-{multiplier}{substituent}{stem}");

            if (random.Next(3) == 0)
            {
                name = string.Create(
                    CultureInfo.InvariantCulture,
                    $"{1 + random.Next(9)}-{Substituents[random.Next(Substituents.Length)]}-{name}");
            }

            if (names.Add(name))
            {
                return name;
            }
        }
    }

    private static string Hill(Dictionary<string, int> counts)
    {
        var builder = new StringBuilder();

        void Append(string symbol)
        {
            if (counts.TryGetValue(symbol, out var count))
            {
                builder.Append(symbol);

                if (count > 1)
                {
                    builder.Append(count.ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        Append("C");
        Append("H");

        foreach (var symbol in counts.Keys.Where(symbol => symbol is not ("C" or "H")).Order(StringComparer.Ordinal))
        {
            Append(symbol);
        }

        return builder.ToString();
    }

    private static async Task<List<string>> WriteShardsAsync<T>(
        string output,
        string kind,
        IEnumerable<T> records,
        int perShard,
        IncrementalHash hash)
    {
        var shards = new List<string>();
        StreamWriter? writer = null;
        var written = 0;

        try
        {
            foreach (var record in records)
            {
                if (writer is null || written == perShard)
                {
                    if (writer is not null)
                    {
                        await writer.DisposeAsync();
                    }

                    var shard = string.Create(CultureInfo.InvariantCulture, $"{kind}-{shards.Count:D4}.jsonl.gz");

                    shards.Add(shard);
                    writer = new StreamWriter(new GZipStream(File.Create(Path.Combine(output, shard)), CompressionLevel.Optimal));
                    written = 0;
                }

                var line = JsonSerializer.Serialize(record, CatalogJson.Options);

                hash.AppendData(Encoding.UTF8.GetBytes(line + "\n"));
                await writer.WriteLineAsync(line);
                written++;
            }
        }
        finally
        {
            if (writer is not null)
            {
                await writer.DisposeAsync();
            }
        }

        return shards;
    }

    private static async Task UploadAsync(string build, string output, string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Uploading needs --connection or ConnectionStrings__blobs.");
        }

        var manifest = JsonSerializer.Deserialize<CatalogBuildManifest>(
            await File.ReadAllTextAsync(Path.Combine(output, CatalogBuildManifest.FileName)), CatalogJson.Options)!;
        var container = new BlobServiceClient(connectionString).GetBlobContainerClient(CatalogBuildManifest.ContainerName);

        await container.CreateIfNotExistsAsync(PublicAccessType.None);

        var stored = container.GetBlobClient($"{build}/{CatalogBuildManifest.FileName}");

        if (await stored.ExistsAsync())
        {
            var existing = (await stored.DownloadContentAsync()).Value.Content.ToObjectFromJson<CatalogBuildManifest>(CatalogJson.Options);

            if (existing?.ContentHash == manifest.ContentHash)
            {
                Console.WriteLine($"Build {build} is already uploaded.");

                return;
            }
        }

        foreach (var shard in manifest.ElementShards.Concat(manifest.SubstanceShards).Concat(manifest.ReactionShards))
        {
            await container.GetBlobClient($"{build}/{shard}").UploadAsync(Path.Combine(output, shard), overwrite: true);
        }

        await stored.UploadAsync(Path.Combine(output, CatalogBuildManifest.FileName), overwrite: true);

        Console.WriteLine($"Uploaded build {build} to {CatalogBuildManifest.ContainerName}.");
    }

    private static async Task<List<T>> ReadAsync<T>(string path)
    {
        var records = new List<T>();

        await foreach (var record in CatalogJson.ReadLinesAsync<T>(path, CancellationToken.None))
        {
            records.Add(record);
        }

        return records;
    }

    private sealed class ZipfSampler
    {
        private readonly double[] _cumulative;
        private readonly Random _random;

        public ZipfSampler(int count, Random random)
        {
            _random = random;
            _cumulative = new double[count];

            var total = 0d;

            for (var rank = 0; rank < count; rank++)
            {
                total += 1d / (rank + 1);
                _cumulative[rank] = total;
            }

            for (var rank = 0; rank < count; rank++)
            {
                _cumulative[rank] /= total;
            }
        }

        public int Next()
        {
            var index = Array.BinarySearch(_cumulative, _random.NextDouble());

            return Math.Min(index >= 0 ? index : ~index, _cumulative.Length - 1);
        }
    }
}
