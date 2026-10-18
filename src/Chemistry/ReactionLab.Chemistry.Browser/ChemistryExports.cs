using System.Numerics;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text.Json;
using ReactionLab.Chemistry.Balancing;
using ReactionLab.Chemistry.Browser.Contracts;
using ReactionLab.Chemistry.Constitution;
using ReactionLab.Chemistry.Formulas;
using ReactionLab.Chemistry.Geometry;

namespace ReactionLab.Chemistry.Browser;

[SupportedOSPlatform("browser")]
internal static partial class ChemistryExports
{
    private const string UnknownElement = "UnknownElement";
    private const string NoAtoms = "NoAtoms";

    private static ChemistryReference? _reference;

    private static ChemistryReference Reference =>
        _reference ?? throw new InvalidOperationException("The chemistry reference data is not loaded.");

    [JSExport]
    internal static void LoadReferenceData(string elementsJson, string referenceJson)
    {
        var elements = JsonSerializer.Deserialize(elementsJson, ChemistryJsonContext.Default.IReadOnlyListElementEntry)
            ?? throw new JsonException("The elements are missing.");
        var document = JsonSerializer.Deserialize(referenceJson, ChemistryJsonContext.Default.ChemistryReferenceDocument)
            ?? throw new JsonException("The chemistry reference is missing.");

        _reference = ChemistryReference.From(elements, document);
    }

    [JSExport]
    internal static string ParseFormula(string formula)
    {
        var result = FormulaParser.TryParse(formula, out var composition, out var error)
            ? new FormulaResult(composition.Hill, composition.Charge, composition.Elements.ToDictionary(element => element.Symbol, element => element.Count, StringComparer.Ordinal), null)
            : new FormulaResult(null, 0, null, error.ToString());

        return JsonSerializer.Serialize(result, ChemistryJsonContext.Default.FormulaResult);
    }

    [JSExport]
    internal static string MolarMassOf(string formula)
    {
        MolarMassResult result;

        if (!FormulaParser.TryParse(formula, out var composition, out var error))
        {
            result = new MolarMassResult(null, error.ToString(), null);
        }
        else if (MolarMass.TryCompute(composition, Reference.AtomicMasses, out var gramsPerMole, out var unknownSymbol))
        {
            result = new MolarMassResult(gramsPerMole, null, null);
        }
        else
        {
            result = new MolarMassResult(null, UnknownElement, unknownSymbol);
        }

        return JsonSerializer.Serialize(result, ChemistryJsonContext.Default.MolarMassResult);
    }

    [JSExport]
    internal static string Balance(string requestJson)
    {
        var request = JsonSerializer.Deserialize(requestJson, ChemistryJsonContext.Default.BalanceRequest)
            ?? throw new JsonException("The balance request is missing.");
        var reactants = new List<ChemicalComposition>();
        var products = new List<ChemicalComposition>();

        foreach (var (formulas, side) in new[] { (request.Reactants, reactants), (request.Products, products) })
        {
            foreach (var formula in formulas)
            {
                if (!FormulaParser.TryParse(formula, out var composition, out var parseError))
                {
                    return JsonSerializer.Serialize(new BalanceResult(null, null, parseError.ToString(), formula), ChemistryJsonContext.Default.BalanceResult);
                }

                side.Add(composition);
            }
        }

        var result = EquationBalancer.TryBalance(reactants, products, out var balanced, out var error)
            ? new BalanceResult(balanced.ReactantCoefficients, balanced.ProductCoefficients, null, null)
            : new BalanceResult(null, null, error.ToString(), null);

        return JsonSerializer.Serialize(result, ChemistryJsonContext.Default.BalanceResult);
    }

    [JSExport]
    internal static string Assemble(string requestJson)
    {
        var request = JsonSerializer.Deserialize(requestJson, ChemistryJsonContext.Default.SkeletonRequest)
            ?? throw new JsonException("The skeleton request is missing.");
        var atoms = request.Atoms.Select(atom => new SkeletalAtom(atom.Symbol, atom.FormalCharge)).ToList();
        var bonds = request.Bonds.Select(bond => new AtomBond(bond.From, bond.To, bond.Order)).ToList();

        var result = MoleculeAssembler.TryAssemble(atoms, bonds, Reference.AtomicGeometry, out var positions, out var error)
            ? new AssemblyResult([.. positions.SelectMany(position => new[] { position.X, position.Y, position.Z })], null)
            : new AssemblyResult(null, error.ToString());

        return JsonSerializer.Serialize(result, ChemistryJsonContext.Default.AssemblyResult);
    }

    [JSExport]
    internal static string DescribeSketch(string requestJson)
    {
        var request = JsonSerializer.Deserialize(requestJson, ChemistryJsonContext.Default.SketchRequest)
            ?? throw new JsonException("The sketch request is missing.");

        return JsonSerializer.Serialize(
            SketchDescriber.Describe(request, Reference), ChemistryJsonContext.Default.SketchDescription);
    }

    [JSExport]
    internal static string StructureKey(string requestJson)
    {
        var request = JsonSerializer.Deserialize(requestJson, ChemistryJsonContext.Default.SketchRequest)
            ?? throw new JsonException("The sketch request is missing.");
        var key = ConstitutionKey.Of(
            [.. request.Atoms.Select(atom => atom.Symbol)],
            [.. request.Bonds.Select(bond => new AtomBond(bond.From, bond.To, bond.Order))]);

        return JsonSerializer.Serialize(new StructureKeyResult(key), ChemistryJsonContext.Default.StructureKeyResult);
    }

    [JSExport]
    internal static string DetectSymmetry(string requestJson)
    {
        var request = JsonSerializer.Deserialize(requestJson, ChemistryJsonContext.Default.SymmetryRequest)
            ?? throw new JsonException("The symmetry request is missing.");

        if (request.PositionsAngstroms.Count != 3 * request.Symbols.Count)
        {
            throw new JsonException("Every atom needs exactly three coordinates.");
        }

        var result = request.Symbols.Count == 0
            ? new SymmetryResult(null, NoAtoms)
            : new SymmetryResult(SymmetryDescriber.Describe(request.Symbols, PositionsOf(request.PositionsAngstroms)), null);

        return JsonSerializer.Serialize(result, ChemistryJsonContext.Default.SymmetryResult);
    }

    [JSExport]
    internal static string Predict(string requestJson)
    {
        var request = JsonSerializer.Deserialize(requestJson, ChemistryJsonContext.Default.PredictionRequest)
            ?? throw new JsonException("The prediction request is missing.");

        return JsonSerializer.Serialize(Reference.Prediction.Predict(request), ChemistryJsonContext.Default.PredictionResult);
    }

    [JSExport]
    internal static string DescribeConditions(string requestJson)
    {
        var request = JsonSerializer.Deserialize(requestJson, ChemistryJsonContext.Default.ConditionsRequest)
            ?? throw new JsonException("The conditions request is missing.");

        return JsonSerializer.Serialize(
            ConditionsDescriber.Describe(request, Reference.StandardStates),
            ChemistryJsonContext.Default.ConditionsResult);
    }

    private static Vector3[] PositionsOf(IReadOnlyList<float> flattened) =>
        [.. Enumerable.Range(0, flattened.Count / 3)
            .Select(atom => new Vector3(flattened[3 * atom], flattened[(3 * atom) + 1], flattened[(3 * atom) + 2]))];
}
