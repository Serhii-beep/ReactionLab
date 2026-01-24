using ReactionLab.Domain.Common;

namespace ReactionLab.Domain.Entities;

public class Tag : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Category { get; set; }

    public ICollection<ReactionTag> ReactionTags { get; set; } = [];
}