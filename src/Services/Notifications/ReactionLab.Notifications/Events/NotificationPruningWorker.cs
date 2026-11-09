using ReactionLab.Messaging.Consuming;
using ReactionLab.Notifications.Persistence;
using ReactionLab.Notifications.Persistence.Mail;
using ReactionLab.Notifications.Persistence.Notices;

namespace ReactionLab.Notifications.Events;

internal sealed class NotificationPruningWorker(
    IServiceProvider services,
    TimeProvider timeProvider,
    ILogger<NotificationPruningWorker> logger) : PruningWorker(timeProvider, logger)
{
    private static readonly TimeSpan NoticeThrottleLifetime = TimeSpan.FromDays(2);
    private static readonly TimeSpan MailRecordLifetime = TimeSpan.FromDays(90);

    protected override async Task PruneAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var queuedBefore = now - NoticeThrottleLifetime;
        var recordedBefore = now - MailRecordLifetime;

        var noticeThrottles = await DeleteAllAsync(
            token => context.DeleteNoticeThrottlesAsync(queuedBefore, token),
            cancellationToken);

        var mailDeliveries = await DeleteAllAsync(
            token => context.DeleteMailDeliveriesAsync(recordedBefore, token),
            cancellationToken);

        var failedMailJobs = await DeleteAllAsync(
            token => context.DeleteFailedMailJobsAsync(recordedBefore, token),
            cancellationToken);

        if (noticeThrottles + mailDeliveries + failedMailJobs > 0)
        {
            Logger.LogInformation(
                "Pruned {NoticeThrottleCount} notice throttle(s), {MailDeliveryCount} mail delivery record(s) "
                + "and {FailedMailJobCount} failed mail job(s).",
                noticeThrottles,
                mailDeliveries,
                failedMailJobs);
        }
    }
}
