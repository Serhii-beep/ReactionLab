using System.ComponentModel.DataAnnotations;

namespace ReactionLab.Notifications.ServiceCalls;

internal sealed class ServiceCallOptions
{
    public const string SectionName = "OpenId";

    [Required]
    public Uri? Issuer { get; set; }

    [Required]
    public string ClientId { get; set; } = string.Empty;

    [Required]
    public string ClientSecret { get; set; } = string.Empty;
}
