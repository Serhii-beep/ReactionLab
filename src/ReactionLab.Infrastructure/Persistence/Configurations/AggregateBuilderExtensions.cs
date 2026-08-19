using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ReactionLab.Infrastructure.Persistence.Configurations;

public static class AggregateBuilderExtensions
{
    public static EntityTypeBuilder<TEntity> HasSearchText<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        builder.Property<string>(PersistenceColumns.SearchText).IsRequired();

        builder.HasIndex(PersistenceColumns.SearchText)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        return builder;
    }

    public static EntityTypeBuilder<TEntity> HasAuditTimestamps<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        builder.Property<DateTimeOffset>(PersistenceColumns.CreatedAt).IsRequired();
        builder.Property<DateTimeOffset>(PersistenceColumns.UpdatedAt).IsRequired();

        return builder;
    }
}
