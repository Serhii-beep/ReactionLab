using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ReactionLab.Identity.Migrator;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddIdentityCore<User>().AddUserStore<RegularAccountUserStore>();
builder.Services.AddSingleton(Options.Create(GatewayClientOptions.Read(builder.Configuration)));
builder.Services.AddScoped<OpenIdRegistration>();
builder.Services.AddScoped<PlatformAdminSeeding>();
builder.Services.AddScoped<DemoDistrictSeeding>();
builder.Services.AddHostedService<MigrationWorker>();

var host = builder.Build();

await host.RunAsync();
