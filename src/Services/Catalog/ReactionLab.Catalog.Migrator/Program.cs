using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReactionLab.Catalog.Infrastructure;
using ReactionLab.Catalog.Migrator;
using ReactionLab.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddAzureBlobServiceClient("blobs");

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddCatalogSeeding(builder.Configuration);
builder.Services.AddCatalogPublishing();
builder.Services.AddHostedService<MigrationWorker>();
builder.Services.AddHostedService<CatalogWorker>();

var host = builder.Build();

await host.RunAsync();
