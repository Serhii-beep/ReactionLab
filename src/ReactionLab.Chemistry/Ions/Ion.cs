namespace ReactionLab.Chemistry.Ions;

public readonly record struct Ion(string Formula, int Charge)
{
    public int Magnitude => Math.Abs(Charge);
}
