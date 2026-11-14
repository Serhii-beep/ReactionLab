using ReactionLab.Notifications.Mail;

namespace ReactionLab.Notifications.Templates;

internal static class MailDevices
{
    public static string Describe(ITranslationCatalog catalog, string locale, string browser, string operatingSystem) =>
        (browser, operatingSystem) switch
        {
            ("", "") => catalog.Translate(locale, "common.unknownDevice"),
            ("", var system) => system,
            (var name, "") => name,
            var (name, system) => catalog.Translate(
                locale,
                "common.deviceDescription",
                new Dictionary<string, object?> { ["browser"] = name, ["system"] = system })
        };
}
