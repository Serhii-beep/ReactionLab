using Microsoft.Extensions.Configuration;

namespace ReactionLab.Identity.Migrator;

internal sealed class GatewayClientOptions
{
    public const string SectionName = "OpenId:Gateway";

    public required string Secret { get; init; }

    public required Uri RedirectUri { get; init; }

    public required Uri PostLogoutRedirectUri { get; init; }

    public static GatewayClientOptions Read(IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);

        return new GatewayClientOptions
        {
            Secret = RequireValue(section, nameof(Secret)),
            RedirectUri = RequireHttpsUri(section, nameof(RedirectUri)),
            PostLogoutRedirectUri = RequireHttpsUri(section, nameof(PostLogoutRedirectUri))
        };
    }

    private static string RequireValue(IConfigurationSection section, string key) =>
        section[key] is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"'{section.Path}:{key}' is not configured.");

    private static Uri RequireHttpsUri(IConfigurationSection section, string key) =>
        Uri.TryCreate(RequireValue(section, key), UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps
            ? uri
            : throw new InvalidOperationException($"'{section.Path}:{key}' must be an absolute HTTPS URI.");
}
