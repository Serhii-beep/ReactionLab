using ReactionLab.Domain.Common;
using ReactionLab.Domain.Enums;
using ReactionLab.Domain.Reactions;
using ReactionLab.Domain.Reactions.Events;
using ReactionLab.Domain.Substances;
using Shouldly;
using Xunit;

namespace ReactionLab.Domain.UnitTests.Reactions;

public sealed class ReactionTests
{
    [Fact]
    public void Create_ProducesAReactionAndRaisesTheCreatedEvent()
    {
        var reaction = WaterSynthesis().Value;

        reaction.Name.ShouldBe("Synthesis of Water");
        reaction.Reactants.Count().ShouldBe(2);
        reaction.Products.Count().ShouldBe(1);
        reaction.DomainEvents.OfType<ReactionCreated>().Count().ShouldBe(1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Create_RejectsMissingName(string? name)
    {
        WaterSynthesis(name).Error.ShouldBe(Reaction.NameRequired);
    }

    [Fact]
    public void Create_RejectsAReactionWithNoReactants()
    {
        var result = Build([CreateProduct("H2O", 2)]);

        result.Error.ShouldBe(Reaction.NoReactants);
    }

    [Fact]
    public void Create_RejectsAReactionWithNoProducts()
    {
        var result = Build([CreateReactant("H2", 1)]);

        result.Error.ShouldBe(Reaction.NoProducts);
    }

    [Fact]
    public void Create_RejectsTheSameSubstanceTwiceOnOneSide()
    {
        var participant = new ParticipantSpecification(SubstanceId.New(), CreateFormula("H2O"), ParticipantRole.Reactant, 1, null);

        var result = Reaction.Create(
            "Duplicate",
            ReactionType.Synthesis,
            [
                participant,
                participant,
                CreateProduct("H2O", 2)
            ],
            DifficultyLevel.Introductory,
            isReversible: false);

        result.Error.ShouldBe(Reaction.DuplicateParticipant);
    }

    [Fact]
    public void Create_AllowsTheSameSubstanceOnBothSides()
    {
        var id = SubstanceId.New();

        var result = Reaction.Create(
            "Duplicate",
            ReactionType.Synthesis,
            [
                new ParticipantSpecification(id, CreateFormula("H2O"), ParticipantRole.Reactant, 1, null),
                new ParticipantSpecification(id, CreateFormula("H2O"), ParticipantRole.Product, 1, null)
            ],
            DifficultyLevel.Introductory,
            isReversible: false);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Create_AcceptsABalancedReaction()
    {
        var result = Build([
            CreateReactant("CH4", 1), CreateReactant("O2", 2),
            CreateProduct("CO2", 1), CreateProduct("H2O", 2)
        ]);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Create_RejectsAnUnbalancedReaction()
    {
        var result = Build([CreateReactant("H2", 1), CreateReactant("O2", 1), CreateProduct("H2O", 1)]);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(Reaction.NotMassBalanced);
    }

    [Fact]
    public void Create_RejectsAnEquationWithAnElementAppearingOnOnlyOneSide()
    {
        var result = Build([CreateReactant("CH4", 1), CreateReactant("O2", 2), CreateProduct("H2O", 2)]);

        result.Error.ShouldBe(Reaction.NotMassBalanced);
    }

    [Fact]
    public void ReactantSignature_IsDistinctAndSorted()
    {
        var reaction = WaterSynthesis().Value;

        var signature = reaction.ReactantSignature;

        signature.Count.ShouldBe(2);
        signature.Select(id => id.Value).ShouldBeInOrder();
        signature.ShouldNotContain(reaction.Products.First().SubstanceId);
    }

    private static ChemicalFormula CreateFormula(string value) => ChemicalFormula.Create(value).Value;

    private static ParticipantSpecification CreateReactant(string formula, int coefficient) =>
        new(SubstanceId.New(), CreateFormula(formula), ParticipantRole.Reactant, coefficient, null);

    private static ParticipantSpecification CreateProduct(string formula, int coefficient) =>
        new(SubstanceId.New(), CreateFormula(formula), ParticipantRole.Product, coefficient, null);

    private static Result<Reaction> Build(IReadOnlyList<ParticipantSpecification> participants) =>
        Reaction.Create(
            "Test",
            ReactionType.Synthesis,
            participants,
            DifficultyLevel.Introductory,
            isReversible: false);

    private static Result<Reaction> WaterSynthesis(string? name = "Synthesis of Water") =>
        Reaction.Create(
            name,
            ReactionType.Synthesis,
            [CreateReactant("H2", 2), CreateReactant("O2", 1), CreateProduct("H2O", 2)],
            DifficultyLevel.Introductory,
            isReversible: false);
}
