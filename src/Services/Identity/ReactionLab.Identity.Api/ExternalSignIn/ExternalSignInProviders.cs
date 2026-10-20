namespace ReactionLab.Identity.Api.ExternalSignIn;

internal sealed class ExternalSignInProviders(IReadOnlyList<IExternalSignInProvider> providers)
{
    private const string SettingsSection = "ExternalSignIn";

    public IReadOnlyList<IExternalSignInProvider> All => providers;

    public static ExternalSignInProviders FromConfiguration(IConfiguration configuration)
    {
        var settings = configuration.GetSection(SettingsSection);

        IExternalSignInProvider?[] configured =
        [
            GoogleSignInProvider.FromSettings(settings.GetSection(GoogleSignInProvider.SettingsSection))
        ];

        return new ExternalSignInProviders([.. configured.OfType<IExternalSignInProvider>()]);
    }

    public IExternalSignInProvider? Find(string? name) =>
        providers.FirstOrDefault(provider => string.Equals(provider.Name, name, StringComparison.Ordinal));
}
