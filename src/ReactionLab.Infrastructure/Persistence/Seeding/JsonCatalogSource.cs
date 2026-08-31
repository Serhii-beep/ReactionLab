using ReactionLab.Domain.Common;
using ReactionLab.Domain.Elements;
using ReactionLab.Domain.Enums;
using ReactionLab.Domain.Localization;
using ReactionLab.Domain.Reactions;
using ReactionLab.Domain.SharedKernel;
using ReactionLab.Domain.Substances;
using ReactionLab.Infrastructure.Persistence.Seeding.Catalog;

namespace ReactionLab.Infrastructure.Persistence.Seeding;

internal sealed class JsonCatalogSource : ICatalogSource
{
    private static readonly Dictionary<string, BondType> BondTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["single"] = BondType.Single,
        ["double"] = BondType.Double,
        ["triple"] = BondType.Triple,
        ["aromatic"] = BondType.Aromatic,
        ["ionic"] = BondType.Ionic,
        ["hydrogen"] = BondType.Hydrogen,
        ["metallic"] = BondType.Metallic
    };

    private readonly List<string> _rejections = [];

    public string Name => "catalog-v1";

    public async Task<CatalogBatch> LoadAsync(CancellationToken cancellationToken)
    {
        _rejections.Clear();

        var directory = Path.Combine(AppContext.BaseDirectory, "catalog");

        var elements = new List<Element>();
        var substances = new List<Substance>();
        var reactions = new List<ReactionSeed>();

        await foreach (var record in Read<ElementRecord>(directory, "elements.jsonl", cancellationToken))
        {
            if (BuildElement(record) is { } element)
            {
                elements.Add(element);
            }
        }

        await foreach (var record in Read<SubstanceRecord>(directory, "substances.jsonl", cancellationToken))
        {
            if (BuildSubstance(record) is { } substance)
            {
                substances.Add(substance);
            }
        }

        await foreach (var record in Read<ReactionRecord>(directory, "reactions.jsonl", cancellationToken))
        {
            if (BuildReaction(record) is { } reaction)
            {
                reactions.Add(reaction);
            }
        }

        return new CatalogBatch(elements, substances, reactions, [.. _rejections]);
    }

    private static IAsyncEnumerable<T> Read<T>(string directory, string file, CancellationToken cancellationToken) =>
        CatalogJson.ReadLinesAsync<T>(Path.Combine(directory, file), cancellationToken);

    private Element? BuildElement(ElementRecord record)
    {
        if (!record.Translations.TryGetValue(SupportedLocale.Default.Code, out var translation))
        {
            Reject(record.Symbol, MissingDefaultLocale());

            return null;
        }

        var content = ElementContent.Create(translation.Name, translation.DiscoveryInfo, translation.InterestingFacts);
        var symbol = ElementSymbol.Create(record.Symbol);
        var number = AtomicNumber.Create(record.AtomicNumber);
        var mass = AtomicMass.Create(record.AtomicMass);
        var position = PeriodicPosition.Create(record.Period, record.Group);
        var color = HexColor.Create(record.DisplayColor);

        if (AnyFailed(record.Symbol, content, symbol, number, mass, position, color))
        {
            return null;
        }

        if (!Enum.TryParse<ElementCategory>(record.Category, ignoreCase: true, out var category)
            || !Enum.TryParse<MatterState>(record.State, ignoreCase: true, out var state))
        {
            Reject(record.Symbol, UnknownValue($"category '{record.Category}' / state '{record.State}'"));

            return null;
        }

        var created = Element.Create(number.Value, symbol.Value, content.Value, mass.Value, category, position.Value, state, color.Value);

        if (created.IsFailure)
        {
            Reject(record.Symbol, created.Error);

            return null;
        }

        var element = created.Value;

        foreach (var (locale, text) in Translations(record.Translations))
        {
            var translated = ElementContent.Create(text.Name, text.DiscoveryInfo, text.InterestingFacts);

            if (translated.IsSuccess)
            {
                element.Translate(locale, translated.Value);
            }
        }

        var physical = element.DescribePhysicalProperties(
            Optional(record.Electronegativity, Electronegativity.Create),
            BuildRadii(record),
            Optional(record.MeltingPointK, Temperature.FromKelvin),
            Optional(record.BoilingPointK, Temperature.FromKelvin));

        if (physical.IsFailure)
        {
            Reject($"{record.Symbol} physical properties", physical.Error);
        }

        element.RecordElectronConfiguration(record.ElectronConfiguration);

        return element;
    }

    private AtomicRadii? BuildRadii(ElementRecord record)
    {
        if (record.CovalentRadiusPm is not { } covalent)
        {
            return null;
        }

        var radii = AtomicRadii.Create(covalent, record.VanDerWaalsRadiusPm);

        if (radii.IsFailure)
        {
            Reject($"{record.Symbol} radii", radii.Error);

            return null;
        }

        return radii.Value;
    }

    private Substance? BuildSubstance(SubstanceRecord record)
    {
        if (!record.Translations.TryGetValue(SupportedLocale.Default.Code, out var translation))
        {
            Reject(record.Formula, MissingDefaultLocale());

            return null;
        }

        var formula = ChemicalFormula.Create(record.Formula);
        var content = SubstanceContent.Create(
            translation.Name, translation.IupacName, translation.Description, translation.SafetyInformation,
            translation.CommonNames, translation.Uses, translation.InterestingFacts);

        if (AnyFailed(record.Formula, formula, content))
        {
            return null;
        }

        if (!Enum.TryParse<SubstanceKind>(record.Kind, ignoreCase: true, out var kind)
            || !Enum.TryParse<MatterState>(record.State, ignoreCase: true, out var state))
        {
            Reject(record.Formula, UnknownValue($"kind '{record.Kind}' / state '{record.State}'"));

            return null;
        }

        var created = Substance.Create(formula.Value, content.Value, kind, record.IsOrganic, state);

        if (created.IsFailure)
        {
            Reject(record.Formula, created.Error);

            return null;
        }

        var substance = created.Value;

        foreach (var (locale, text) in Translations(record.Translations))
        {
            var translated = SubstanceContent.Create(
                text.Name, text.IupacName, text.Description, text.SafetyInformation,
                text.CommonNames, text.Uses, text.InterestingFacts);

            if (translated.IsSuccess)
            {
                substance.Translate(locale, translated.Value);
            }
        }

        substance.Classify(record.Category);

        if (record.MolecularWeight is { } grams)
        {
            var weight = MolecularWeight.Create(grams);

            if (weight.IsSuccess)
            {
                substance.RecordWeight(weight.Value);
            }
        }

        if (BuildStructure(record) is { } structure)
        {
            var defined = substance.DefineStructure(structure);

            if (defined.IsFailure)
            {
                Reject($"{record.Formula} structure", defined.Error);
            }
        }

        return substance;
    }

    private MolecularStructure? BuildStructure(SubstanceRecord record)
    {
        if (record.Structure is not { Atoms.Count: > 0 } source)
        {
            return null;
        }

        var atoms = new List<AtomPlacement>(source.Atoms.Count);

        foreach (var atom in source.Atoms)
        {
            var symbol = ElementSymbol.Create(atom.Symbol);

            if (symbol.IsFailure)
            {
                Reject($"{record.Formula} structure", symbol.Error);

                return null;
            }

            atoms.Add(new AtomPlacement(symbol.Value, atom.X, atom.Y, atom.Z));
        }

        var bonds = source.Bonds
            .Select(bond => new Bond(
                bond.From,
                bond.To,
                BondTypes.GetValueOrDefault(bond.Type, BondType.Single)))
            .ToList();

        var structure = MolecularStructure.Create(atoms, bonds);

        if (structure.IsFailure)
        {
            Reject($"{record.Formula} structure", structure.Error);

            return null;
        }

        return structure.Value;
    }

    private ReactionSeed? BuildReaction(ReactionRecord record)
    {
        if (!record.Translations.TryGetValue(SupportedLocale.Default.Code, out var translation))
        {
            Reject(record.Key, MissingDefaultLocale());

            return null;
        }

        var content = ReactionContent.Create(
            translation.Name, translation.Description, translation.Mechanism,
            translation.SafetyWarnings, translation.RealWorldExamples);
        var difficulty = DifficultyLevel.Create(record.Difficulty);

        if (AnyFailed(record.Key, content, difficulty))
        {
            return null;
        }

        if (!Enum.TryParse<ReactionType>(record.Type, ignoreCase: true, out var type))
        {
            Reject(record.Key, UnknownValue($"reaction type '{record.Type}'"));

            return null;
        }

        var participants = new List<ParticipantSeed>(record.Participants.Count);

        foreach (var participant in record.Participants)
        {
            if (!Enum.TryParse<ParticipantRole>(participant.Role, ignoreCase: true, out var role))
            {
                Reject(record.Key, UnknownValue($"participant role '{participant.Role}'"));

                return null;
            }

            participants.Add(new ParticipantSeed(
                participant.Formula,
                role,
                participant.Coefficient,
                Enum.TryParse<MatterState>(participant.State, ignoreCase: true, out var state) ? state : null,
                participant.Substance));
        }

        var energetics = Thermodynamics.Create(
            Optional(record.EnthalpyKjPerMol, Enthalpy.FromKilojoulesPerMole),
            record.ActivationEnergyKjPerMol);

        if (energetics.IsFailure)
        {
            Reject($"{record.Key} energetics", energetics.Error);
        }

        var conditions = ReactionConditions.Create(
            Optional(record.TemperatureK, Temperature.FromKelvin), pressure: null, record.Catalyst);

        var visualization = VisualizationHint.Create(record.EffectPreset, record.AnimationDurationMs);

        var provenance = record.Rule is null
            ? ReactionProvenance.Curated
            : ReactionProvenance.Create(record.Rule, record.Confidence ?? 1m);

        if (provenance.IsFailure)
        {
            Reject($"{record.Key} provenance", provenance.Error);
        }

        return new ReactionSeed(
            record.Key,
            content.Value,
            type,
            difficulty.Value,
            record.IsReversible,
            participants,
            energetics.IsSuccess ? energetics.Value : null,
            conditions.IsSuccess ? conditions.Value : null,
            visualization.IsSuccess ? visualization.Value : null,
            provenance.IsSuccess ? provenance.Value : ReactionProvenance.Curated,
            record.Tags ?? []);
    }

    private static IEnumerable<(SupportedLocale Locale, TText Text)> Translations<TText>(Dictionary<string, TText> translations)
    {
        foreach (var (code, text) in translations)
        {
            if (string.Equals(code, SupportedLocale.Default.Code, StringComparison.Ordinal))
            {
                continue;
            }

            var locale = SupportedLocale.Create(code);

            if (locale.IsSuccess)
            {
                yield return (locale.Value, text);
            }
        }
    }

    private static TValue? Optional<TRaw, TValue>(TRaw? raw, Func<TRaw, Result<TValue>> create)
        where TRaw : struct
        where TValue : class
    {
        if (raw is not { } value)
        {
            return null;
        }

        var created = create(value);

        return created.IsSuccess ? created.Value : null;
    }

    private static Error MissingDefaultLocale() => Error.Validation(
        "Catalog.MissingDefaultLocale",
        $"The record has no '{SupportedLocale.Default.Code}' translation.");

    private static Error UnknownValue(string what) =>
        Error.Validation("Catalog.UnknownValue", $"The catalog contains and unrecognized {what}.");

    private bool AnyFailed(string key, params Result[] results)
    {
        var failed = false;

        foreach (var result in results.Where(result => result.IsFailure))
        {
            Reject(key, result.Error);
            failed = true;
        }

        return failed;
    }

    private void Reject(string key, Error error) =>
        _rejections.Add($"{key}: {error.Code} - {error.Description}");
}
