using System.Collections.Concurrent;

namespace ReactionLab.Identity.Api.ExternalSignIn;

internal sealed class ProviderDiscovery(ExternalSignInProviders providers)
{
    private readonly ConcurrentDictionary<string, bool> _isDiscoveredByProvider = new(
        providers.All.Select(provider => KeyValuePair.Create(provider.Name, false)));

    public bool IsPending(string providerName) =>
        _isDiscoveredByProvider.TryGetValue(providerName, out var isDiscovered) && !isDiscovered;

    public void Complete(string providerName) => _isDiscoveredByProvider[providerName] = true;
}
