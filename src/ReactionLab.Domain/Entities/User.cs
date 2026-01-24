using ReactionLab.Domain.Common;
using ReactionLab.Domain.Enums;

namespace ReactionLab.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public UserRole Role { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<UserExperiment> Experiments { get; set; } = [];

    public ICollection<UserProgress> Progress { get; set; } = [];
}