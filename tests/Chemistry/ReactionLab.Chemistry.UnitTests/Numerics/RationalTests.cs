using System.Numerics;
using ReactionLab.Chemistry.Numerics;
using Shouldly;
using Xunit;

namespace ReactionLab.Chemistry.UnitTests.Numerics;

public sealed class RationalTests
{
    [Theory]
    [InlineData(6, 8, 3, 4)]
    [InlineData(10, 5, 2, 1)]
    [InlineData(0, 7, 0, 1)]
    [InlineData(1, -2, -1, 2)]
    [InlineData(-1, -2, 1, 2)]
    public void From_NormalisesToLowestTermsWithTheSignOnTheNumerator(
        int numerator, int denominator, int expectedNumerator, int expectedDenominator)
    {
        var value = Rational.From(numerator, denominator);

        value.Numerator.ShouldBe(expectedNumerator);
        value.Denominator.ShouldBe(expectedDenominator);
    }

    [Fact]
    public void From_RejectsAZeroDenominator() =>
        Should.Throw<DivideByZeroException>(() => _ = Rational.From(1, 0));

    [Fact]
    public void Division_RejectsZero() =>
        Should.Throw<DivideByZeroException>(() => _ = Rational.One / Rational.Zero);

    [Fact]
    public void Arithmetic_StaysExact()
    {
        var third = Rational.From(1, 3);

        (third + third + third).ShouldBe(Rational.One);
        (third - third).ShouldBe(Rational.Zero);
        (Rational.From(1, 10) + Rational.From(2, 10)).ShouldBe(Rational.From(3, 10));
        (Rational.From(2, 3) * Rational.From(3, 2)).ShouldBe(Rational.One);
        (third / third).ShouldBe(Rational.One);
    }

    [Fact]
    public void Default_IsZero()
    {
        var defaultRational = default(Rational);

        defaultRational.IsZero.ShouldBeTrue();
        defaultRational.Denominator.ShouldBe(BigInteger.One);
        (defaultRational + Rational.One).ShouldBe(Rational.One);
    }
}
