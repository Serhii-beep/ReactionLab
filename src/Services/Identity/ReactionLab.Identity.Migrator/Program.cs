using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReactionLab.Identity.Migrator;
using ReactionLab.Identity.Persistence;
using ReactionLab.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddHostedService<MigrationWorker>();

var host = builder.Build();

await host.RunAsync();
