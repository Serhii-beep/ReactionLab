using ReactionLab.Domain.Common;

namespace ReactionLab.Domain.Entities;

public class UserProgress : BaseEntity
{
    public Guid UserId { get; set; }

    public Guid ReactionId { get; set; }

    public bool Completed { get; set; }

    public DateTime? CompletedAt { get; set; }

    public int AttemptCount { get; set; }

    public int? BestScore { get; set; }

    public User User { get; set; } = null!;

    public Reaction Reaction { get; set; } = null!;
}