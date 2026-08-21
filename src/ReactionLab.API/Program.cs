using ReactionLab.API;
using ReactionLab.API.Endpoints.Elements;
using ReactionLab.Application;
using ReactionLab.Infrastructure;
using ReactionLab.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApi(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();

app.UseHttpsRedirection();
app.UseCors(ReactionLab.API.DependencyInjection.CorsPolicy);

app.MapDefaultEndpoints();
app.MapOpenApi();

var api = app.MapGroup("/api/v1");
api.MapElementEndpoints();

await app.RunAsync();
