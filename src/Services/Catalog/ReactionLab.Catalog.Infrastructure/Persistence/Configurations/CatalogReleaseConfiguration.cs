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

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.HasIndex(r => r.Status);

        builder.Property(r => r.AnswerVersion).IsRequired();
        builder.Property(r => r.ChangeMark).IsRequired();

        builder.Property(r => r.PublishedAt).IsRequired();
        builder.HasIndex(r => r.PublishedAt);

        builder.Property(r => r.ActivatedAt);
        builder.Property(r => r.RetiredAt);

        builder.Ignore(r => r.DomainEvents);
    }
}
