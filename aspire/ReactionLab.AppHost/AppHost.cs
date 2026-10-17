var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("reactionlab-pgdata")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgAdmin();

var catalogDatabase = postgres.AddDatabase("reactionlab");

var cache = builder.AddRedis("cache")
    .WithDataVolume("reactionlab-redisdata")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithRedisCommander();

var catalogMigrator = builder.AddProject<Projects.ReactionLab_Catalog_Migrator>("catalog-migrator")
    .WithReference(catalogDatabase, connectionName: "DefaultConnection")
    .WaitFor(catalogDatabase);

var catalog = builder.AddProject<Projects.ReactionLab_Catalog_Api>("catalog")
    .WithReference(catalogDatabase, connectionName: "DefaultConnection")
    .WithReference(cache, connectionName: "Redis")
    .WaitForCompletion(catalogMigrator)
    .WaitFor(cache);

var gateway = builder.AddProject<Projects.ReactionLab_Gateway>("gateway")
    .WithReference(catalog)
    .WithExternalHttpEndpoints();

if (builder.ExecutionContext.IsRunMode)
{
    gateway.WithEnvironment("ReverseProxy__Clusters__catalog__HttpRequest__ActivityTimeout", "01:00:00");
}

await builder.Build().RunAsync();
