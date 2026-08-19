var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("reactionlab-pgdata")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgAdmin();

var database = postgres.AddDatabase("reactionlab");

var cache = builder.AddRedis("cache")
    .WithDataVolume("reactionlab-redisdata")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithRedisCommander();

var migrator = builder.AddProject<Projects.ReactionLab_DbMigrator>("migrator")
    .WithReference(database, connectionName: "DefaultConnection")
    .WaitFor(database);

builder.AddProject<Projects.ReactionLab_API>("api")
    .WithReference(database, connectionName: "DefaultConnection")
    .WithReference(cache, connectionName: "Redis")
    .WaitForCompletion(migrator)
    .WaitFor(cache);

await builder.Build().RunAsync();
