using System.ComponentModel.DataAnnotations;

namespace ReactionLab.Notifications.Sending;

internal sealed class MailOptions
{
    public const string SectionName = "Mail";

    [Required]
    public Uri? SiteUri { get; set; }

    [Required]
    public string FromAddress { get; set; } = string.Empty;

    [Required]
    public string FromName { get; set; } = string.Empty;

    public bool RequireTls { get; set; } = true;
}
