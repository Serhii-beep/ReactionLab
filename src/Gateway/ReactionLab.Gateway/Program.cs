using ReactionLab.Gateway;
using ReactionLab.ServiceDefaults;
using ReactionLab.Web;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddErrorContract();
builder.Services.AddRateLimiter(ClientAddressCeiling.Configure);
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 64 * 1024);

var app = builder.Build();

app.UseForwardedHeaders();
app.UseErrorContract();

app.UseHttpsRedirection();
app.UseRateLimiter();

app.MapDefaultEndpoints();
app.MapReverseProxy();

await app.RunAsync();
