const string SyntheticBuild = "synthetic-100k-1m";

var builder = DistributedApplication.CreateBuilder(args);

var completeVolume = string.Equals(builder.Configuration["CatalogVolume"], "complete", StringComparison.OrdinalIgnoreCase);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("reactionlab-pgdata")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgAdmin();

var catalogDatabase = postgres.AddDatabase("catalog-db", completeVolume ? "catalog_complete" : "catalog");
var identityDatabase = postgres.AddDatabase("identity-db", "identity");

var gatewayClientSecret = builder.AddParameter(
    "gateway-client-secret",
    new GenerateParameterDefault { MinLength = 32, Special = false },
    secret: true,
    persist: true);

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

if (completeVolume)
{
    var synthesizer = builder.AddProject<Projects.ReactionLab_CatalogBuilder>("catalog-synthesizer")
        .WithArgs("synthesize", "--upload", "--build", SyntheticBuild)
        .WithReference(blobs)
        .WaitFor(blobs);

    catalogMigrator
        .WithEnvironment("CatalogImport__Build", SyntheticBuild)
        .WaitForCompletion(synthesizer);
}

var catalog = builder.AddProject<Projects.ReactionLab_Catalog_Api>("catalog")
    .WithReference(catalogDatabase, connectionName: "DefaultConnection")
    .WithReference(cache, connectionName: "Redis")
    .WaitForCompletion(catalogMigrator)
    .WaitFor(cache);

var identityMigrator = builder.AddProject<Projects.ReactionLab_Identity_Migrator>("identity-migrator")
    .WithReference(identityDatabase, connectionName: "DefaultConnection")
    .WaitFor(identityDatabase);

var identity = builder.AddProject<Projects.ReactionLab_Identity_Api>("identity")
    .WithReference(identityDatabase, connectionName: "DefaultConnection")
    .WaitForCompletion(identityMigrator);

var gateway = builder.AddProject<Projects.ReactionLab_Gateway>("gateway")
    .WithReference(catalog)
    .WithReference(identity)
    .WithExternalHttpEndpoints();

var gatewayAddress = gateway.GetEndpoint("https");

identity.WithEnvironment("OpenId__Issuer", gatewayAddress);

identityMigrator
    .WithEnvironment("OpenId__Gateway__Secret", gatewayClientSecret)
    .WithEnvironment("OpenId__Gateway__RedirectUri", ReferenceExpression.Create($"{gatewayAddress}/signin-oidc"))
    .WithEnvironment(
        "OpenId__Gateway__PostLogoutRedirectUri",
        ReferenceExpression.Create($"{gatewayAddress}/signout-callback-oidc"));

if (builder.Configuration["Identity:FirstPlatformAdminEmail"] is { Length: > 0 } firstPlatformAdminEmail)
{
    identityMigrator.WithEnvironment("Identity__FirstPlatformAdminEmail", firstPlatformAdminEmail);
}

if (builder.Configuration["Identity:BreakGlassPlatformAdminAccountId"] is { Length: > 0 } breakGlassAccountId)
{
    identityMigrator.WithEnvironment("Identity__BreakGlassPlatformAdminAccountId", breakGlassAccountId);
}

if (builder.ExecutionContext.IsRunMode)
{
    gateway.WithEnvironment("ReverseProxy__Clusters__catalog__HttpRequest__ActivityTimeout", "01:00:00")
        .WithEnvironment("ReverseProxy__Clusters__identity__HttpRequest__ActivityTimeout", "01:00:00")
        .WithEnvironment(
            "ReverseProxy__Clusters__releases__Destinations__storage__Address",
            ReferenceExpression.Create($"{storage.Resource.BlobUriExpression}/devstoreaccount1/catalog-releases"));

    var demoPassword = builder.AddParameter(
        "demo-password",
        new GenerateParameterDefault { MinLength = 32, Special = false },
        secret: true,
        persist: true);

    identityMigrator.WithEnvironment("Identity__DemoPassword", demoPassword);
}

await builder.Build().RunAsync();
