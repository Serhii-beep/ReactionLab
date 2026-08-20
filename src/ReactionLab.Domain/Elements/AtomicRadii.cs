using ReactionLab.Domain.Common;

namespace ReactionLab.Domain.Elements;

public sealed record AtomicRadii
{
    public const decimal MaximumPicometers = 400m;

    public static readonly Error NotPositive = Error.Validation(
        "AtomicRadii.NotPositive",
        "A radius must be greater than zero.");

    public static readonly Error Implausible = Error.Validation(
        "AtomicRadii.Implausible",
        $"A radius above {MaximumPicometers} pm exceeds any known element.")
        .WithArgs(("max", MaximumPicometers));

    public static readonly Error VanDerWaalsSmallerThanCovalent = Error.Validation(
        "AtomicRadii.VanDerWaalsSmallerThanCovalent",
        "Van der Waals radius cannot be smaller than covalent radius.");

    private AtomicRadii(decimal covalentPicometers, decimal? vanDerWaalsPicometers)
    {
        CovalentPicometers = covalentPicometers;
        VanDerWaalsPicometers = vanDerWaalsPicometers;
    }

    public decimal CovalentPicometers { get; }

    public decimal? VanDerWaalsPicometers { get; }

    public static Result<AtomicRadii> Create(decimal covalentPicometers, decimal? vanDerWaalsPicometers = null)
    {
        if (covalentPicometers <= 0 || vanDerWaalsPicometers <= 0)
        {
            return NotPositive;
        }

        if (covalentPicometers > MaximumPicometers || vanDerWaalsPicometers > MaximumPicometers)
        {
            return Implausible;
        }

        if (vanDerWaalsPicometers is not null && vanDerWaalsPicometers < covalentPicometers)
        {
            return VanDerWaalsSmallerThanCovalent;
        }

        return new AtomicRadii(covalentPicometers, vanDerWaalsPicometers);
    }
}
