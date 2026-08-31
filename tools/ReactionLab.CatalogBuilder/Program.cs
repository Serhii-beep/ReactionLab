using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using ReactionLab.CatalogBuilder;
using ReactionLab.Chemistry.Formulas;
using ReactionLab.Domain.Elements;
using ReactionLab.Domain.Reactions;
using ReactionLab.Domain.Substances;
using ReactionLab.Infrastructure.Persistence.Seeding.Catalog;

var root = FindRepositoryRoot();

if (args.Contains("generate", StringComparer.OrdinalIgnoreCase))
{
    CandidateGeneration.Run(root);
    return;
}

if (args.Contains("emit", StringComparer.OrdinalIgnoreCase))
{
    await CatalogEmission.RunAsync(root);
    return;
}

if (args.Contains("rmsd", StringComparer.OrdinalIgnoreCase))
{
    GeometryAccuracy.Run(root);

    return;
}

var sources = Path.Combine(root, "data", "sources");
var output = Path.Combine(root, "data", "catalog", "v1");

using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60), BaseAddress = new Uri("https://pubchem.ncbi.nlm.nih.gov/rest/pug/") };
http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ReactionLab", "0.1"));

var elements = await ReadJsonAsync<List<SourceElement>>(Path.Combine(sources, "elements.json"));
var atomicMasses = elements.ToDictionary(
    element => element.Symbol, element => element.AtomicMass, StringComparer.Ordinal);

await BuildElementsAsync();
await BuildSubstancesAsync();

Console.WriteLine($"Catalog written to {output}");

async Task BuildElementsAsync()
{
    var records = elements.Select(element => new ElementRecord
    {
        AtomicNumber = element.AtomicNumber,
        Symbol = element.Symbol,
        AtomicMass = element.AtomicMass,
        Category = element.Category,
        Period = element.Period,
        Group = element.Group,
        State = element.StateAtRoomTemp,
        DisplayColor = element.DisplayColor,
        ElectronConfiguration = element.ElectronConfiguration,
        Electronegativity = element.Electronegativity,
        MeltingPointK = element.MeltingPoint,
        BoilingPointK = element.BoilingPoint,
        Translations = new Dictionary<string, ElementRecord.ElementText>
        {
            ["en"] = new(element.Name, element.DiscoveryInfo, element.InterestingFacts)
        }
    }).ToList();

    await CatalogJson.WriteLinesAsync(Path.Combine(output, "elements.jsonl"), records, CancellationToken.None);
    Console.WriteLine($"elements: {records.Count}");
}

async Task BuildSubstancesAsync()
{
    var wanted = (await File.ReadAllLinesAsync(Path.Combine(sources, "substances.txt")))
        .Select(line => line.Trim())
        .Where(line => line.Length > 0 && !line.StartsWith('#'))
        .Select(ParseWanted)
        .ToList();

    var records = new List<SubstanceRecord>();
    var skipped = new List<string>();

    foreach (var (index, wantedSubstance) in wanted.Index())
    {
        var record = await FetchSubstanceAsync(wantedSubstance);

        if (record is null)
        {
            skipped.Add(wantedSubstance.Name);
            Console.WriteLine($"   [{index + 1}/{wanted.Count}] {wantedSubstance.Name}: not found");

            continue;
        }

        records.Add(record);
        Console.WriteLine(
            $"   [{index + 1}/{wanted.Count}] {wantedSubstance.Name} -> {record.Formula}" +
            $"{(record.Structure is null ? " (no 3D conformer)" : $" ({record.Structure.Atoms.Count} atoms)")}");
    }

    await CatalogJson.WriteLinesAsync(Path.Combine(output, "substances.jsonl"), records, CancellationToken.None);
    Console.WriteLine($"substances: {records.Count}" + (skipped.Count > 0 ? $" ({skipped.Count} not found: {string.Join(", ", skipped)})" : string.Empty));
}

async Task<SubstanceRecord?> FetchSubstanceAsync(WantedSubstance wanted)
{
    var found = await LookUpAsync(wanted.Name, wanted.Formula);

    if (found is not { Verified: true } && wanted.Formula is not null)
    {
        found = await LookUpAsync(wanted.Formula, wanted.Formula) ?? found;
    }

    if (found is null)
    {
        return null;
    }

    if (!found.Verified)
    {
        Console.WriteLine($"      unverified: declared {wanted.Formula}, CID {found.Cid} is {found.Properties.Formula} - dropping its identity");
    }

    var sdf = found.Verified ? await GetTextAsync($"compound/cid/{found.Cid}/SDF?record_type=3d") : null;

    return new SubstanceRecord
    {
        Formula = wanted.Formula ?? found.Properties.Formula,
        Kind = wanted.Kind,
        State = wanted.State,
        IsOrganic = Chemistry.IsOrganic(wanted.Formula ?? found.Properties.Formula, wanted.Category),
        MolecularWeight = found.Verified
            ? found.Properties.MolecularWeight
            : Chemistry.MolecularWeight(wanted.Formula!, atomicMasses),
        Category = wanted.Category,
        Structure = sdf is null ? null : ParseSdf(sdf),
        PubChemCid = found.Verified ? found.Cid : null,
        Translations = new Dictionary<string, SubstanceRecord.SubstanceText>
        {
            ["en"] = new(
                Capitalize(wanted.Name), found.Verified ? found.Properties.IupacName : null,
                null, null, null, null, null)
        }
    };
}

async Task<PubChemMatch?> LookUpAsync(string key, string? declared)
{
    var cid = await GetCidAsync(key);

    if (cid is null)
    {
        return null;
    }

    var properties = await GetPropertiesAsync(cid.Value);

    return properties is null
        ? null
        : new PubChemMatch(
            cid.Value,
            properties,
            declared is null || Chemistry.SameComposition(declared, properties.Formula));
}

async Task<int?> GetCidAsync(string name)
{
    var json = await GetTextAsync($"compound/name/{Uri.EscapeDataString(name)}/cids/JSON");

    if (json is null)
    {
        return null;
    }

    using var document = JsonDocument.Parse(json);

    return document.RootElement.GetProperty("IdentifierList").GetProperty("CID")
        .EnumerateArray().Select(element => element.GetInt32()).FirstOrDefault();
}

async Task<PubChemProperties?> GetPropertiesAsync(int cid)
{
    var json = await GetTextAsync(
        $"compound/cid/{cid}/property/MolecularFormula,MolecularWeight,IUPACName/JSON");

    if (json is null)
    {
        return null;
    }

    using var document = JsonDocument.Parse(json);
    var row = document.RootElement.GetProperty("PropertyTable").GetProperty("Properties")[0];

    var weight = row.TryGetProperty("MolecularWeight", out var raw) ? raw.GetString() : null;

    return new PubChemProperties(
        row.GetProperty("MolecularFormula").GetString()!,
        decimal.TryParse(weight, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed : null,
        row.TryGetProperty("IUPACName", out var iupac) ? iupac.GetString() : null);
}

async Task<string?> GetTextAsync(string path)
{
    for (var attempt = 0; attempt < 4; attempt++)
    {
        await Task.Delay(250);

        using var response = await http.GetAsync(path);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStringAsync();
        }

        if (response.StatusCode is HttpStatusCode.NotFound)
        {
            return null;
        }

        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
    }

    return null;
}

static SubstanceRecord.StructureRecord? ParseSdf(string sdf)
{
    var lines = sdf.Split('\n');

    if (lines.Length < 5
        || !int.TryParse(lines[3].AsSpan(0, 3), NumberStyles.Integer, CultureInfo.InvariantCulture, out var atomCount)
        || !int.TryParse(lines[3].AsSpan(3, 3), NumberStyles.Integer, CultureInfo.InvariantCulture, out var bondCount)
        || atomCount == 0)
    {
        return null;
    }

    var atoms = new List<SubstanceRecord.AtomRecord>(atomCount);

    for (var i = 0; i < atomCount; i++)
    {
        var line = lines[4 + i];

        atoms.Add(new SubstanceRecord.AtomRecord(
            line[31..34].Trim(),
            Coordinate(line[0..10]), Coordinate(line[10..20]), Coordinate(line[20..30])));
    }

    var bonds = new List<SubstanceRecord.BondRecord>(bondCount);

    for (var i = 0; i < bondCount; i++)
    {
        var line = lines[4 + atomCount + i];

        bonds.Add(new SubstanceRecord.BondRecord(
            Index(line[0..3]) - 1,
            Index(line[3..6]) - 1,
            Index(line[6..9]) switch
            {
                2 => "double",
                3 => "triple",
                4 => "aromatic",
                _ => "single"
            }));
    }

    return new SubstanceRecord.StructureRecord(atoms, bonds);

    static double Coordinate(string field) =>
        double.Parse(field, NumberStyles.Float, CultureInfo.InvariantCulture);

    static int Index(string field) => int.Parse(field, CultureInfo.InvariantCulture);
}

static WantedSubstance ParseWanted(string line)
{
    var parts = line.Split('|', StringSplitOptions.TrimEntries);

    return new WantedSubstance(
        parts[0],
        Capitalize(Field(parts, 1) ?? "solid"),
        Capitalize(Field(parts, 2) ?? "molecular"),
        Field(parts, 3),
        Field(parts, 4));

    static string? Field(string[] source, int index) =>
        source.Length > index && source[index].Length > 0 ? source[index] : null;
}

static string Capitalize(string value) =>
    value.Length == 0 ? value : char.ToUpperInvariant(value[0]) + value[1..];

static async Task<T> ReadJsonAsync<T>(string path)
{
    await using var stream = File.OpenRead(path);

    return (await JsonSerializer.DeserializeAsync<T>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }))!;
}

static string FindRepositoryRoot()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);

    while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ReactionLab.slnx")))
    {
        directory = directory.Parent;
    }

    return directory?.FullName ?? throw new InvalidOperationException("ReactionLab.slnx not found.");
}

internal sealed record WantedSubstance(string Name, string State, string Kind, string? Category, string? Formula);

internal sealed record PubChemProperties(string Formula, decimal? MolecularWeight, string? IupacName);

internal sealed record PubChemMatch(int Cid, PubChemProperties Properties, bool Verified);

internal sealed record SourceElement(
    int AtomicNumber, string Symbol, string Name, decimal AtomicMass, string Category, int Period,
    int? Group, string? ElectronConfiguration, decimal? Electronegativity, decimal? MeltingPoint,
    decimal? BoilingPoint, string StateAtRoomTemp, string DisplayColor, string? DiscoveryInfo,
    List<string>? InterestingFacts);

internal sealed record SourceReaction(
    string Name, string ReactionType, decimal? RequiredTemperature, string? CatalystInfo, decimal? EnthalpyChange,
    decimal? ActivationEnergy, string? EffectPreset, int? AnimationDurationMs, string? Description,
    string? Mechanism, List<string>? RealWorldExamples, string? SafetyWarnings, int DifficultyLevel,
    List<SourceParticipant>? Participants, List<string>? Tags);

internal sealed record SourceParticipant(string MoleculeFormula, string Role, int Coefficient, string? State, string? Substance);

internal static class Chemistry
{
    private static readonly HashSet<string> OrganicCategories = new(StringComparer.OrdinalIgnoreCase)
      {
          "alkane", "alkene", "alkyne", "aromatic", "alcohol", "aldehyde", "ketone", "ester",
          "ether", "halocarbon", "organic-acid", "organic", "sugar", "amino-acid", "nucleobase",
          "lipid", "vitamin", "pharmaceutical", "stimulant", "alkaloid", "neurotransmitter", "terpene"
      };

    private static readonly HashSet<string> InorganicCarbon = new(StringComparer.Ordinal)
      {
          "CO", "CO2", "H2CO3", "HCN", "NaHCO3", "Na2CO3", "CaCO3", "KHCO3", "K2CO3"
      };

    public static bool IsOrganic(string formula, string? category)
    {
        if (category is not null)
        {
            return OrganicCategories.Contains(category);
        }

        if (InorganicCarbon.Contains(formula))
        {
            return false;
        }

        var parsed = ChemicalFormula.Create(formula);

        return parsed.IsSuccess
            && parsed.Value.CountOf(ElementSymbol.Create("C").Value) > 0
            && parsed.Value.CountOf(ElementSymbol.Create("H").Value) > 0;
    }

    public static bool SameComposition(string declared, string reported)
    {
        var left = ChemicalFormula.Create(declared);
        var right = ChemicalFormula.Create(reported);

        return left.IsSuccess && right.IsSuccess && string.Equals(left.Value.Hill, right.Value.Hill, StringComparison.Ordinal);
    }

    public static decimal? MolecularWeight(string formula, IReadOnlyDictionary<string, decimal> atomicMasses) =>
        FormulaParser.TryParse(formula, out var composition, out _)
        && MolarMass.TryCompute(composition, atomicMasses, out var grams, out _)
            ? decimal.Round(grams, 3)
            : null;
}
