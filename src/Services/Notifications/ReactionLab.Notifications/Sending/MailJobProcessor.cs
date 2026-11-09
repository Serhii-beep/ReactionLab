using Microsoft.Extensions.Options;
using MimeKit;
using ReactionLab.Identity.Contracts.Contacts.V1;
using ReactionLab.Notifications.Mail;
using ReactionLab.Notifications.Persistence;
using ReactionLab.Notifications.Persistence.Mail;
using ReactionLab.Notifications.ServiceCalls;
using ReactionLab.Notifications.Templates;

namespace ReactionLab.Notifications.Sending;

internal sealed class MailJobProcessor(
    AppDbContext context,
    ContactDirectory contacts,
    MailTemplates templates,
    ITranslationCatalog catalog,
    MailRenderer renderer,
    IOptions<MailOptions> options,
    MailMetrics metrics,
    TimeProvider timeProvider,
    ILogger<MailJobProcessor> logger)
{
    private const int MaximumAttempts = 10;

    private static readonly TimeSpan JobBudget = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan GraceAfterStop = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan FirstRetryDelay = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan LongestRetryDelay = TimeSpan.FromHours(1);

    public async Task<bool> ProcessAsync(
        MailJob job,
        SmtpMailer mailer,
        TimeSpan? pauseOnSystemicFailure,
        CancellationToken stoppingToken)
    {
        using var activity = MailTelemetry.StartSend(job);

        var (outcome, failedSystemically) = await SendAsync(job, mailer, pauseOnSystemicFailure, stoppingToken);

        activity?.SetTag(MailTelemetry.OutcomeTag, outcome);
        metrics.RecordFinished(job.MailType, outcome);

        return failedSystemically;
    }

    private async Task<(string Outcome, bool FailedSystemically)> SendAsync(
        MailJob job,
        SmtpMailer mailer,
        TimeSpan? pauseOnSystemicFailure,
        CancellationToken stoppingToken)
    {
        if (job.Attempts > MaximumAttempts)
        {
            return (await FailAsync(job, MailFailures.AttemptsExhausted, exception: null), false);
        }

        using var budget = new CancellationTokenSource(JobBudget, timeProvider);
        using var lookup = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, budget.Token);
        using var grace = stoppingToken.Register(() => budget.CancelAfter(GraceAfterStop));

        try
        {
            var template = templates.Find(job.MailType);

            if (job.CreatedAt + template.Lifetime < timeProvider.GetUtcNow())
            {
                return (await DropAsync(job, MailJobOutcomes.Expired), false);
            }

            var contact = await contacts.FindAsync(job.RecipientId, lookup.Token);

            if (contact is null)
            {
                return (await DropAsync(job, MailJobOutcomes.RecipientGone), false);
            }

            if (!contact.HasEmail)
            {
                return (await DropAsync(job, MailJobOutcomes.NoAddress), false);
            }

            var displayName = contact.EmailConfirmed ? contact.DisplayName : null;

            if (!MailAddresses.TryCreateMailbox(displayName, contact.Email, out var to))
            {
                return (await DropAsync(job, MailJobOutcomes.UnsendableAddress), false);
            }

            var rendered = await ComposeAsync(template, contact, displayName);

            using var message = MailMessages.Create(
                job.Id,
                new MailboxAddress(options.Value.FromName, options.Value.FromAddress),
                to,
                rendered);

            await mailer.SendAsync(message, budget.Token);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
        {
            return await SettleFailureAsync(job, ex, pauseOnSystemicFailure);
        }

        await context.FinishSentMailJobAsync(job, CancellationToken.None);
        metrics.RecordSent(job.MailType, timeProvider.GetUtcNow() - job.CreatedAt);

        return (MailJobOutcomes.Sent, false);
    }

    private async Task<RenderedMail> ComposeAsync(IMailTemplate template, Contact contact, string? displayName)
    {
        try
        {
            var recipient = new MailRecipient(displayName, contact.Email, catalog.ResolveLocale(contact.Locale));

            return await renderer.RenderAsync(template.Compose(recipient));
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            throw new MailCompositionException(ex);
        }
    }

    private async Task<string> DropAsync(MailJob job, string outcome)
    {
        await context.DropMailJobAsync(job, CancellationToken.None);

        logger.LogInformation(
            "Mail job {MailJobId} of type {MailType} was dropped unsent: {Outcome}.",
            job.Id,
            job.MailType,
            outcome);

        return outcome;
    }

    private async Task<(string Outcome, bool FailedSystemically)> SettleFailureAsync(
        MailJob job,
        Exception exception,
        TimeSpan? pauseOnSystemicFailure)
    {
        var failure = MailFailures.Describe(exception);
        var kind = MailFailures.Classify(exception);
        var loggedException = MailFailures.MayQuoteAddress(exception) ? null : exception;

        if (kind == MailFailureKind.Permanent)
        {
            return (await FailAsync(job, failure, loggedException), false);
        }

        if (kind == MailFailureKind.Systemic && pauseOnSystemicFailure is { } pause)
        {
            await context.DeferMailJobAsync(job, pause, failure, CancellationToken.None);

            logger.LogWarning(
                loggedException,
                "Mail job {MailJobId} of type {MailType} waits out the pause, its attempt given back: {Failure}.",
                job.Id,
                job.MailType,
                failure);

            return (MailJobOutcomes.Deferred, true);
        }

        if (job.Attempts >= MaximumAttempts)
        {
            return (await FailAsync(job, failure, loggedException), kind == MailFailureKind.Systemic);
        }

        var retryDelay = RetryDelay(job.Attempts);

        await context.RetryMailJobAsync(job, retryDelay, failure, CancellationToken.None);

        logger.LogWarning(
            loggedException,
            "Mail job {MailJobId} of type {MailType} failed on attempt {Attempts}: {Failure}. "
            + "Next try in {RetryDelay}.",
            job.Id,
            job.MailType,
            job.Attempts,
            failure,
            retryDelay);

        return (MailJobOutcomes.Retried, kind == MailFailureKind.Systemic);
    }

    private async Task<string> FailAsync(MailJob job, string failure, Exception? exception)
    {
        await context.FailMailJobAsync(job, failure, CancellationToken.None);

        logger.LogError(
            exception,
            "Mail job {MailJobId} of type {MailType} failed terminally after {Attempts} attempt(s): {Failure}.",
            job.Id,
            job.MailType,
            job.Attempts,
            failure);

        return MailJobOutcomes.Failed;
    }

    private static TimeSpan RetryDelay(int attempts)
    {
        var exponential = FirstRetryDelay * Math.Pow(2, Math.Clamp(attempts - 1, 0, 10));
        var capped = exponential < LongestRetryDelay ? exponential : LongestRetryDelay;

        return capped * (0.8 + (0.4 * Random.Shared.NextDouble()));
    }
}
