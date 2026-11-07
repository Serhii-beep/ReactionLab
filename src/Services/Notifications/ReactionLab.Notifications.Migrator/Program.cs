using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReactionLab.Notifications.Migrator;
using ReactionLab.Notifications.Persistence;
using ReactionLab.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddHostedService<MigrationWorker>();

var host = builder.Build();

await host.RunAsync();
