using ReactionLab.Identity.Contracts;
using ReactionLab.Messaging;
using ReactionLab.Messaging.Consuming;
using ReactionLab.Notifications.Events;
using ReactionLab.Notifications.Persistence;
using ReactionLab.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddScoped<RepeatedRegistrationNotices>();
builder.Services.AddScoped<AccountErasure>();
builder.Services.AddHostedService<NotificationPruningWorker>();
builder.AddEventConsumer<AppDbContext, NotificationEventEffects>(
    "kafka",
    "notifications",
    [IdentityTopics.Users, IdentityTopics.Erasure],
    NotificationEventEffects.EventTypes,
    NotificationEventEffects.ErasureEventTypes);

using var host = builder.Build();

if (args is [DeadLetterReplay<AppDbContext>.CommandName])
{
    var replay = host.Services.GetRequiredService<DeadLetterReplay<AppDbContext>>();

    Environment.ExitCode = await replay.ReplayAllAsync(CancellationToken.None) ? 0 : 1;

    return;
}

await host.RunAsync();
