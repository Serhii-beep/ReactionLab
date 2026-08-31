namespace ReactionLab.CatalogBuilder;

internal sealed record Candidate(
    string Signature,
    string Rule,
    decimal Confidence,
    decimal? MinimumKelvin,
    decimal? EnthalpyKjPerMol,
    decimal? ActivationEnergyKjPerMol,
    List<CandidateParticipant> Reactants,
    List<CandidateParticipant> Products)
{
    public string Family => Rule.Split('.')[0];

    public string Variant => Rule.Split('.')[^1];

    public IEnumerable<CandidateParticipant> All => Reactants.Concat(Products);
}

internal sealed record CandidateParticipant(string Formula, int Coefficient, string? Phase);
