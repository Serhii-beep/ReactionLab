namespace ReactionLab.Infrastructure.Persistence.Seeding;

public interface IDataSeeder
{
    int Order { get; }

    Task SeedAsync(CancellationToken cancellationToken = default);
}
