using System.Globalization;
using System.Numerics;

namespace ReactionLab.Chemistry.Numerics;

public readonly struct Rational : IEquatable<Rational>
{
    public static readonly Rational Zero = new(BigInteger.Zero, BigInteger.One);

    public static readonly Rational One = new(BigInteger.One, BigInteger.One);

    private readonly BigInteger _denominator;

    private Rational(BigInteger numerator, BigInteger denominator)
    {
        Numerator = numerator;
        _denominator = denominator;
    }

    public BigInteger Numerator { get; }

    public BigInteger Denominator => _denominator.IsZero ? BigInteger.One : _denominator;

    public bool IsZero => Numerator.IsZero;

    public int Sign => Numerator.Sign;

    public static Rational From(BigInteger value) => new(value, BigInteger.One);

    public static Rational From(BigInteger numerator, BigInteger denominator)
    {
        if (denominator.IsZero)
        {
            throw new DivideByZeroException("A rational cannot have a zero denominator.");
        }

        if (numerator.IsZero)
        {
            return Zero;
        }

        if (denominator.Sign < 0)
        {
            numerator = -numerator;
            denominator = -denominator;
        }

        var divisor = BigInteger.GreatestCommonDivisor(BigInteger.Abs(numerator), denominator);

        return new Rational(numerator / divisor, denominator / divisor);
    }

    public static Rational operator +(Rational left, Rational right) => From(
        left.Numerator * right.Denominator + right.Numerator * left.Denominator, left.Denominator * right.Denominator);

    public static Rational operator -(Rational left, Rational right) => left + -right;

    public static Rational operator -(Rational value) => new(-value.Numerator, value.Denominator);

    public static Rational operator *(Rational left, Rational right) =>
        From(left.Numerator * right.Numerator, left.Denominator * right.Denominator);

    public static Rational operator /(Rational left, Rational right) => right.IsZero
        ? throw new DivideByZeroException("Cannot divide a rational by zero.")
        : From(left.Numerator * right.Denominator, left.Denominator * right.Numerator);

    public static bool operator ==(Rational left, Rational right) => left.Equals(right);

    public static bool operator !=(Rational left, Rational right) => !left.Equals(right);

    public bool Equals(Rational other) => Numerator == other.Numerator && Denominator == other.Denominator;

    public override bool Equals(object? obj) => obj is Rational other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Numerator, Denominator);

    public override string ToString() => Denominator.IsOne
        ? Numerator.ToString(CultureInfo.InvariantCulture)
        : string.Create(CultureInfo.InvariantCulture, $"{Numerator}/{Denominator}");
}
