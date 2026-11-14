using MyCSharp.HttpUserAgentParser;

namespace ReactionLab.Identity.Api.ServiceCalls;

internal static class DeviceDescriptions
{
    public static DeviceDescription FromUserAgent(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return new DeviceDescription(string.Empty, string.Empty);
        }

        var parsed = HttpUserAgentParser.Parse(userAgent);
        var browser = parsed.Type == HttpUserAgentType.Browser ? parsed.Name ?? string.Empty : string.Empty;

        return new DeviceDescription(browser, OperatingSystemOf(parsed.Platform));
    }

    private static string OperatingSystemOf(HttpUserAgentPlatformInformation? platform) =>
        platform is not { } known
            ? string.Empty
            : known.PlatformType switch
            {
                HttpUserAgentPlatformType.Windows => "Windows",
                HttpUserAgentPlatformType.MacOS => "macOS",
                HttpUserAgentPlatformType.IOS => "iOS",
                HttpUserAgentPlatformType.Android => "Android",
                HttpUserAgentPlatformType.Linux => "Linux",
                HttpUserAgentPlatformType.ChromeOS => "ChromeOS",
                HttpUserAgentPlatformType.Unknown => string.Empty,
                _ => known.Name ?? string.Empty
            };
}
