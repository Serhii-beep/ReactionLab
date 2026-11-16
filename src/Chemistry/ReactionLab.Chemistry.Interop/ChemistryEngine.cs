using System.Numerics;
using System.Text.Json;
using ReactionLab.Chemistry.Balancing;
using ReactionLab.Chemistry.Constitution;
using ReactionLab.Chemistry.Formulas;
using ReactionLab.Chemistry.Geometry;
using ReactionLab.Chemistry.Interop.Contracts;

namespace ReactionLab.Chemistry.Interop;

public sealed class ChemistryEngine
{
    private const string UnknownElement = "UnknownElement";
    private const string NoAtoms = "NoAtoms";

    private readonly ChemistryReference _reference;

    private ChemistryEngine(ChemistryReference reference) => _reference = reference;

    public static ChemistryEngine Load(string elementsJson, string referenceJson)
    {
        var elements = JsonSerializer.Deserialize(elementsJson, ChemistryJsonContext.Default.IReadOnlyListElementEntry)
            ?? throw new JsonException("The elements are missing.");
        var document =
            JsonSerializer.Deserialize(referenceJson, ChemistryJsonContext.Default.ChemistryReferenceDocument)
            ?? throw new JsonException("The chemistry reference is missing.");

        return new ChemistryEngine(ChemistryReference.From(elements, document));
    }

    public static string ParseFormula(string formula)
    {
        var result = FormulaParser.TryParse(formula, out var composition, out var error)
            ? new FormulaResult(
                composition.Hill,
                composition.Charge,
                composition.Elements.ToDictionary(
                    element => element.Symbol, element => element.Count, StringComparer.Ordinal),
                null)
            : new FormulaResult(null, 0, null, error.ToString());

        return JsonSerializer.Serialize(result, ChemistryJsonContext.Default.FormulaResult);
    }

    public string MolarMassOf(string formula)
    {
        MolarMassResult result;

        if (!FormulaParser.TryParse(formula, out var composition, out var error))
        {
            result = new MolarMassResult(null, error.ToString(), null);
        }
        else if (MolarMass.TryCompute(
            composition, _reference.AtomicMasses, out var gramsPerMole, out var unknownSymbol))
        {
            result = new MolarMassResult(gramsPerMole, null, null);
        }
        else
        {
            result = new MolarMassResult(null, UnknownElement, unknownSymbol);
        }

        return JsonSerializer.Serialize(result, ChemistryJsonContext.Default.MolarMassResult);
    }

    public static string Balance(string requestJson)
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
                    return JsonSerializer.Serialize(
                        new BalanceResult(null, null, parseError.ToString(), formula),
                        ChemistryJsonContext.Default.BalanceResult);
                }

                side.Add(composition);
            }
        }

        var result = EquationBalancer.TryBalance(reactants, products, out var balanced, out var error)
            ? new BalanceResult(balanced.ReactantCoefficients, balanced.ProductCoefficients, null, null)
            : new BalanceResult(null, null, error.ToString(), null);

        return JsonSerializer.Serialize(result, ChemistryJsonContext.Default.BalanceResult);
    }

    public string Assemble(string requestJson)
    {
        var request = JsonSerializer.Deserialize(requestJson, ChemistryJsonContext.Default.SkeletonRequest)
            ?? throw new JsonException("The skeleton request is missing.");
        var atoms = request.Atoms.Select(atom => new SkeletalAtom(atom.Symbol, atom.FormalCharge)).ToList();
        var bonds = request.Bonds.Select(bond => new AtomBond(bond.From, bond.To, bond.Order)).ToList();

        var result = MoleculeAssembler.TryAssemble(
                atoms, bonds, _reference.AtomicGeometry, out var positions, out var error)
            ? new AssemblyResult(
                [.. positions.SelectMany(position => new[] { position.X, position.Y, position.Z })],
                null)
            : new AssemblyResult(null, error.ToString());

        return JsonSerializer.Serialize(result, ChemistryJsonContext.Default.AssemblyResult);
    }

    public string DescribeSketch(string requestJson)
    {
        var request = JsonSerializer.Deserialize(requestJson, ChemistryJsonContext.Default.SketchRequest)
            ?? throw new JsonException("The sketch request is missing.");

        return JsonSerializer.Serialize(
            SketchDescriber.Describe(request, _reference), ChemistryJsonContext.Default.SketchDescription);
    }

    public static string StructureKey(string requestJson)
    {
        var request = JsonSerializer.Deserialize(requestJson, ChemistryJsonContext.Default.SketchRequest)
            ?? throw new JsonException("The sketch request is missing.");
        var key = ConstitutionKey.Of(
            [.. request.Atoms.Select(atom => atom.Symbol)],
            [.. request.Bonds.Select(bond => new AtomBond(bond.From, bond.To, bond.Order))]);

        return JsonSerializer.Serialize(new StructureKeyResult(key), ChemistryJsonContext.Default.StructureKeyResult);
    }

    public static string DetectSymmetry(string requestJson)
    {
        var request = JsonSerializer.Deserialize(requestJson, ChemistryJsonContext.Default.SymmetryRequest)
            ?? throw new JsonException("The symmetry request is missing.");

        if (request.PositionsAngstroms.Count != 3 * request.Symbols.Count)
        {
            throw new JsonException("Every atom needs exactly three coordinates.");
        }

        var result = request.Symbols.Count == 0
            ? new SymmetryResult(null, NoAtoms)
            : new SymmetryResult(
                SymmetryDescriber.Describe(request.Symbols, PositionsOf(request.PositionsAngstroms)),
                null);

        return JsonSerializer.Serialize(result, ChemistryJsonContext.Default.SymmetryResult);
    }

    public string Predict(string requestJson)
    {
        var request = JsonSerializer.Deserialize(requestJson, ChemistryJsonContext.Default.PredictionRequest)
            ?? throw new JsonException("The prediction request is missing.");

        return JsonSerializer.Serialize(
            _reference.Prediction.Predict(request), ChemistryJsonContext.Default.PredictionResult);
    }

    public string DescribeConditions(string requestJson)
    {
        var request = JsonSerializer.Deserialize(requestJson, ChemistryJsonContext.Default.ConditionsRequest)
            ?? throw new JsonException("The conditions request is missing.");

        return JsonSerializer.Serialize(
            ConditionsDescriber.Describe(request, _reference.StandardStates),
            ChemistryJsonContext.Default.ConditionsResult);
    }

    private static Vector3[] PositionsOf(IReadOnlyList<float> flattened) =>
        [.. Enumerable.Range(0, flattened.Count / 3)
            .Select(atom => new Vector3(flattened[3 * atom], flattened[(3 * atom) + 1], flattened[(3 * atom) + 2]))];
}
