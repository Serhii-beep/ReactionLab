using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReactionLab.DbMigrator;
using ReactionLab.Infrastructure;
using ReactionLab.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<MigrationWorker>();

var host = builder.Build();

await host.RunAsync();
