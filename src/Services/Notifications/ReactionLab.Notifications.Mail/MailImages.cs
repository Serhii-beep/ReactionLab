namespace ReactionLab.Notifications.Mail;

public static class MailImages
{
    public const string LogoContentId = "logo";
    public const string LogoFileName = "logo.png";

    public static Stream OpenLogo() => new MemoryStream(Logo, writable: false);

    private static readonly byte[] Logo = Read(LogoFileName);

    private static byte[] Read(string resourceName)
    {
        using var stream = typeof(MailImages).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"The mail image '{resourceName}' isn't embedded.");
        using var copy = new MemoryStream();

        stream.CopyTo(copy);

        return copy.ToArray();
    }
}
