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

var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(emulator => emulator
        .WithDataVolume("reactionlab-storagedata")
        .WithLifetime(ContainerLifetime.Persistent));

var blobs = storage.AddBlobs("blobs");

var catalogMigrator = builder.AddProject<Projects.ReactionLab_Catalog_Migrator>("catalog-migrator")
    .WithReference(catalogDatabase, connectionName: "DefaultConnection")
    .WithReference(blobs)
    .WaitFor(catalogDatabase)
    .WaitFor(blobs);

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
    gateway.WithEnvironment("ReverseProxy__Clusters__catalog__HttpRequest__ActivityTimeout", "01:00:00")
        .WithEnvironment(
            "ReverseProxy__Clusters__releases__Destinations__storage__Address",
            ReferenceExpression.Create($"{storage.Resource.BlobUriExpression}/devstoreaccount1/catalog-releases"));
}

await builder.Build().RunAsync();
