using ReactionLab.Catalog.Api;
using ReactionLab.Catalog.Api.Endpoints;
using ReactionLab.Catalog.Application;
using ReactionLab.Catalog.Infrastructure;
using ReactionLab.ServiceDefaults;
using ReactionLab.Web;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApi();

builder.WebHost.ConfigureKestrel(options =>
    options.Limits.MaxRequestBodySize = 64 * 1024);

var app = builder.Build();

app.UseForwardedHeaders();
app.UseErrorContract();

app.UseHttpsRedirection();
app.UseResponseCompression();

app.UseRateLimiter();

app.MapDefaultEndpoints();
app.MapOpenApi();

if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
}

var api = app.MapGroup("/api/v1");
api.MapElementEndpoints();
api.MapSubstanceEndpoints();
api.MapReactionEndpoints();
api.MapChemistryEndpoints();

await app.RunAsync();
