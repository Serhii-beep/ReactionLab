using OpenTelemetry.Logs;
using ReactionLab.Identity.Api;
using ReactionLab.Identity.Api.Endpoints;
using ReactionLab.Identity.Api.Features.Erasures;
using ReactionLab.Identity.Api.Http;
using ReactionLab.Identity.Contracts;
using ReactionLab.Identity.Persistence;
using ReactionLab.Messaging;
using ReactionLab.Messaging.Consuming;
using ReactionLab.ServiceDefaults;
using ReactionLab.Web;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddPersistence(builder.Configuration);
builder.AddOutboxRelay<AppDbContext>("kafka", IdentityEventTypes.Source);
builder.AddEventConsumer<AppDbContext, ErasureConfirmations>(
    "kafka",
    "identity",
    ErasureConfirmations.Topics,
    ErasureConfirmations.EventTypes,
    [ErasureEventTypes.UserDataErased]);
builder.Services.Configure<EventConsumerOptions>(options => options.StopHostOnFailure = false);
builder.Services.AddAccounts();
builder.Services.AddOpenIdServer(builder.Configuration, builder.Environment);
builder.Services.AddExternalSignIn(builder.Configuration, builder.Environment);
builder.Services.AddPlatformRoles();
builder.Services.AddOrganizations();
builder.Services.AddSchoolAdministration();
builder.Services.AddErasures();
builder.Services.AddApi();

builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 64 * 1024);

var app = builder.Build();

if (args is [DeadLetterReplay<AppDbContext>.CommandName])
{
    var replay = app.Services.GetRequiredService<DeadLetterReplay<AppDbContext>>();

    Environment.ExitCode = await replay.ReplayAllAsync(CancellationToken.None) ? 0 : 1;

    app.Services.GetService<LoggerProvider>()?.ForceFlush();
    await app.DisposeAsync();

    return;
}

app.UseForwardedHeaders();
app.UseErrorContract();

app.UseHttpsRedirection();
app.UseExternalSignInAdmission();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapDefaultEndpoints();
app.MapOpenApi();

if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
}

app.MapOpenIdEndpoints();
app.MapServiceCallEndpoints();

var api = app.MapGroup("/api/v1");
api.MapAccountEndpoints();
api.MapAccountSettingsEndpoints();
api.MapSessionEndpoints();
api.MapLoginEndpoints();
api.MapExternalSignInEndpoints();
api.MapPlatformRoleEndpoints();
api.MapOrganizationEndpoints();
api.MapSchoolAdministrationEndpoints();
api.MapAccountOrganizationEndpoints();
api.MapAccountSetupEndpoints();

await app.RunAsync();
