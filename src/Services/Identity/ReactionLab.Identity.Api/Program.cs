using ReactionLab.Identity.Api;
using ReactionLab.Identity.Api.Endpoints;
using ReactionLab.Identity.Persistence;
using ReactionLab.ServiceDefaults;
using ReactionLab.Web;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddAccounts();
builder.Services.AddOpenIdServer(builder.Configuration, builder.Environment);
builder.Services.AddApi();

builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 64 * 1024);

var app = builder.Build();

app.UseForwardedHeaders();
app.UseErrorContract();

app.UseHttpsRedirection();
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

var api = app.MapGroup("/api/v1");
api.MapAccountEndpoints();
api.MapAccountSettingsEndpoints();
api.MapSessionEndpoints();

await app.RunAsync();
