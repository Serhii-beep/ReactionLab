using ReactionLab.API;
using ReactionLab.API.Endpoints;
using ReactionLab.Application;
using ReactionLab.Infrastructure;
using ReactionLab.ServiceDefaults;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApi(builder.Configuration);

builder.WebHost.ConfigureKestrel(options =>
    options.Limits.MaxRequestBodySize = 64 * 1024);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseCors(ReactionLab.API.DependencyInjection.CorsPolicy);

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
