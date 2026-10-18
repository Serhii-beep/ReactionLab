using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactionLab.Catalog.Domain.Releases;

namespace ReactionLab.Catalog.Infrastructure.Persistence.Configurations;

internal sealed class CatalogReleaseConfiguration : IEntityTypeConfiguration<CatalogRelease>
{
    public void Configure(EntityTypeBuilder<CatalogRelease> builder)
    {
        builder.ToTable("catalog_releases");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.Version)
            .HasMaxLength(CatalogRelease.VersionLength)
            .IsRequired();

        builder.Property(r => r.PublishedAt).IsRequired();
        builder.HasIndex(r => r.PublishedAt);

        builder.Ignore(r => r.DomainEvents);
    }
}
