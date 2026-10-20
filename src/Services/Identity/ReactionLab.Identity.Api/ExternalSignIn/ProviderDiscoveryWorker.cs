using Microsoft.Extensions.Options;
using OpenIddict.Client;

namespace ReactionLab.Identity.Api.ExternalSignIn;

internal sealed class ProviderDiscoveryWorker(
    IOptionsMonitor<OpenIddictClientOptions> clientOptions,
    ProviderDiscovery discovery,
    TimeProvider timeProvider,
    ILogger<ProviderDiscoveryWorker> logger) : BackgroundService
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(30);

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.WhenAll(clientOptions.CurrentValue.Registrations.Select(registration =>
            DiscoverAsync(registration, stoppingToken)));

    private async Task DiscoverAsync(OpenIddictClientRegistration registration, CancellationToken stoppingToken)
    {
        var providerName = registration.ProviderName!;

        while (true)
        {
            try
            {
                await registration.ConfigurationManager!.GetConfigurationAsync(stoppingToken).WaitAsync(stoppingToken);
                discovery.Complete(providerName);

                logger.LogInformation("Read {Provider}'s configuration. Its sign-in is available.", providerName);

                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(
                    ex,
                    "{Provider}'s configuration couldn't be read. Its sign-in is unavailable.",
                    providerName);
            }

            await Task.Delay(RetryDelay, timeProvider, stoppingToken);
        }
    }
}
