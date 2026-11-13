using ReactionLab.Identity.Contracts;
using ReactionLab.Messaging;
using ReactionLab.Messaging.Consuming;
using ReactionLab.Notifications;
using ReactionLab.Notifications.Events;
using ReactionLab.Notifications.Persistence;
using ReactionLab.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.Configure<HostOptions>(options => options.ServicesStopConcurrently = true);

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddScoped<RepeatedRegistrationNotices>();
builder.Services.AddScoped<EmailConfirmationRequests>();
builder.Services.AddScoped<AccountErasure>();
builder.Services.AddHostedService<NotificationPruningWorker>();
builder.AddEventConsumer<AppDbContext, NotificationEventEffects>(
    "kafka",
    "notifications",
    [IdentityTopics.Users, IdentityTopics.Erasure],
    NotificationEventEffects.EventTypes,
    NotificationEventEffects.ErasureEventTypes);
builder.AddIdentityCalls();
builder.AddMailSending();

using var host = builder.Build();

if (args is [DeadLetterReplay<AppDbContext>.CommandName])
{
    var replay = host.Services.GetRequiredService<DeadLetterReplay<AppDbContext>>();

    Environment.ExitCode = await replay.ReplayAllAsync(CancellationToken.None) ? 0 : 1;

    return;
}

try
{
    await host.RunAsync();
}
catch (Exception ex)
{
    await Console.Error.WriteLineAsync(ex.ToString());
    Environment.ExitCode = 1;
}
