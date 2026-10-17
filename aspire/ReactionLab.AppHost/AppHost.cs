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

builder.AddProject<Projects.ReactionLab_Catalog_Api>("catalog")
    .WithReference(catalogDatabase, connectionName: "DefaultConnection")
    .WithReference(cache, connectionName: "Redis")
    .WaitForCompletion(catalogMigrator)
    .WaitFor(cache);

await builder.Build().RunAsync();
