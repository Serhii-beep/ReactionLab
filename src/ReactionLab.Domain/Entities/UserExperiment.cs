using ReactionLab.Domain.Common;

namespace ReactionLab.Domain.Entities;

public class UserExperiment : BaseEntity
{
    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? WorkspaceState { get; set; } // JSON: full 3D scene state

    public string? ThumbnailUrl { get; set; }

    public bool IsPublic { get; set; }

    public User User { get; set; } = null!;
}