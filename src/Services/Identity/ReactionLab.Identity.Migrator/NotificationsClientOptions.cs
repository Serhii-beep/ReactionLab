using Microsoft.Extensions.Configuration;

namespace ReactionLab.Identity.Migrator;

internal sealed class NotificationsClientOptions
{
    public const string SectionName = "OpenId:Notifications";

    public required string Secret { get; init; }

    public static NotificationsClientOptions Read(IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);

        return new NotificationsClientOptions
        {
            Secret = section[nameof(Secret)] is { Length: > 0 } secret
                ? secret
                : throw new InvalidOperationException($"'{section.Path}:{nameof(Secret)}' is not configured.")
        };
    }
}
