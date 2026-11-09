using ReactionLab.Notifications.Mail;
using ReactionLab.Notifications.Persistence;
using ReactionLab.Notifications.Persistence.Mail;
using ReactionLab.Notifications.Templates;

namespace ReactionLab.Notifications.Sending;

internal sealed class MailJobWorker(
    IServiceScopeFactory scopeFactory,
    SmtpEndpoint smtpEndpoint,
    MailTemplates templates,
    ITranslationCatalog catalog,
    MailRenderer renderer,
    TimeProvider timeProvider,
    ILogger<MailJobWorker> logger) : BackgroundService, IHostedLifecycleService
{
    private const int BatchSize = 10;
    private const int SystemicFailuresBeforePause = 2;
    private const string SampleAddress = "sample@example.com";

    private static readonly TimeSpan Lease = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan FailureDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan FirstPause = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan LongestPause = TimeSpan.FromMinutes(5);
    private static readonly string?[] SampleDisplayNames = ["Sample", null];

    private int _systemicFailuresInARow;
    private TimeSpan _pause = FirstPause;

    public Task StartingAsync(CancellationToken cancellationToken) => ComposeEveryTemplateAsync();

    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = IdleDelay;

            try
            {
                var batch = await SendBatchAsync(stoppingToken);

                if (batch.Paused)
                {
                    logger.LogWarning("Sending mail pauses for {Pause}: Identity or the mail server failed.", _pause);
                    delay = _pause;
                    _pause = _pause * 2 < LongestPause ? _pause * 2 : LongestPause;
                }
                else if (batch.Claimed == BatchSize)
                {
                    continue;
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "A batch of mail jobs failed.");
                delay = FailureDelay;
            }

            await Task.Delay(delay, timeProvider, stoppingToken);
        }
    }

    private async Task ComposeEveryTemplateAsync()
    {
        foreach (var template in templates.All)
        {
            foreach (var locale in catalog.Locales)
            {
                foreach (var displayName in SampleDisplayNames)
                {
                    await renderer.RenderAsync(
                        template.Compose(new MailRecipient(displayName, SampleAddress, locale)));
                }
            }
        }
    }

    private async Task<MailBatchOutcome> SendBatchAsync(CancellationToken stoppingToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var processor = scope.ServiceProvider.GetRequiredService<MailJobProcessor>();
        var jobs = await context.ClaimDueMailJobsAsync(Lease, BatchSize, stoppingToken);

        if (jobs.Count == 0)
        {
            return new MailBatchOutcome(Claimed: 0, Paused: false);
        }

        var leasedUntil = jobs[0].AvailableAt;

        await using var mailer = new SmtpMailer(smtpEndpoint);

        for (var index = 0; index < jobs.Count; index++)
        {
            try
            {
                stoppingToken.ThrowIfCancellationRequested();

                var failedSystemically = await processor.ProcessAsync(
                    jobs[index],
                    mailer,
                    PauseOnSystemicFailure(),
                    stoppingToken);

                if (!failedSystemically)
                {
                    _systemicFailuresInARow = 0;
                    _pause = FirstPause;
                    continue;
                }

                _systemicFailuresInARow++;

                if (_systemicFailuresInARow < SystemicFailuresBeforePause)
                {
                    continue;
                }

                await ReleaseAsync(context, jobs, index + 1, leasedUntil);

                return new MailBatchOutcome(jobs.Count, Paused: true);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                await ReleaseAsync(context, jobs, index, leasedUntil);
                throw;
            }
        }

        return new MailBatchOutcome(jobs.Count, Paused: false);
    }

    private TimeSpan? PauseOnSystemicFailure() =>
        _systemicFailuresInARow + 1 >= SystemicFailuresBeforePause ? _pause : null;

    private static Task ReleaseAsync(AppDbContext context, List<MailJob> jobs, int first, DateTimeOffset leasedUntil) =>
        first < jobs.Count
            ? context.ReleaseMailJobsAsync(
                jobs.GetRange(first, jobs.Count - first),
                leasedUntil,
                CancellationToken.None)
            : Task.CompletedTask;
}
