namespace ReactionLab.Domain.Entities;

public class ReactionTag
{
    public Guid ReactionId { get; set; }

    public Guid TagId { get; set; }

    public Reaction Reaction { get; set; } = null!;

    public Tag Tag { get; set; } = null!;
}