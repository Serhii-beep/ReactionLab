using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactionLab.Identity.Persistence.Organizations;

namespace ReactionLab.Identity.Persistence.Configurations;

internal sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("organizations", table =>
        {
            table.HasCheckConstraint(
                "ck_organizations_kind",
                $"kind IN ('{OrganizationKinds.District}', '{OrganizationKinds.School}')");

            table.HasCheckConstraint(
                "ck_organizations_district_id",
                $"kind = '{OrganizationKinds.School}' OR district_id IS NULL");
        });

        builder.Property(organization => organization.Id).ValueGeneratedNever();
        builder.Property(organization => organization.Kind).HasMaxLength(Organization.MaximumKindLength).IsRequired();
        builder.Property(organization => organization.Name).HasMaxLength(Organization.MaximumNameLength).IsRequired();
        builder.Property(organization => organization.CreatedAt).IsRequired();

        builder.Property(organization => organization.SignInCode)
            .HasMaxLength(Organization.MaximumSignInCodeLength)
            .IsRequired();

        builder.HasIndex(organization => organization.SignInCode).IsUnique();
        builder.HasIndex(organization => new { organization.DistrictId, organization.Id });

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(organization => organization.DistrictId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
