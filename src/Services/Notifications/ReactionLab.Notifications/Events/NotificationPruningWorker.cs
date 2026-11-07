using ReactionLab.Messaging.Consuming;
using ReactionLab.Notifications.Persistence;
using ReactionLab.Notifications.Persistence.Notices;

namespace ReactionLab.Notifications.Events;

internal sealed class NotificationPruningWorker(
    IServiceProvider services,
    TimeProvider timeProvider,
    ILogger<NotificationPruningWorker> logger) : PruningWorker(timeProvider, logger)
{
    private static readonly TimeSpan NoticeThrottleLifetime = TimeSpan.FromDays(2);

    protected override async Task PruneAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var queuedBefore = now - NoticeThrottleLifetime;

        var noticeThrottles = await DeleteAllAsync(
            token => context.DeleteNoticeThrottlesAsync(queuedBefore, token),
            cancellationToken);

        if (noticeThrottles > 0)
        {
            Logger.LogInformation("Pruned {NoticeThrottleCount} notice throttle(s).", noticeThrottles);
        }
    }
}
