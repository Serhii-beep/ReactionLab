using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.Identity.Persistence.Organizations;

namespace ReactionLab.Identity.Persistence.Configurations;

internal sealed class UserSchoolRoleConfiguration : IEntityTypeConfiguration<UserSchoolRole>
{
    public void Configure(EntityTypeBuilder<UserSchoolRole> builder)
    {
        builder.ToTable("user_school_roles", table => table.HasCheckConstraint(
            "ck_user_school_roles_role",
            $"role IN ('{SchoolRoleNames.SchoolAdmin}', '{SchoolRoleNames.Teacher}', '{SchoolRoleNames.Student}', "
                + $"'{SchoolRoleNames.DistrictAdmin}')"));

        builder.HasKey(schoolRole => new { schoolRole.UserId, schoolRole.OrganizationId, schoolRole.Role });
        builder.Property(schoolRole => schoolRole.Role).HasMaxLength(UserSchoolRole.MaximumRoleLength);
        builder.HasIndex(schoolRole => new { schoolRole.OrganizationId, schoolRole.Role });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(schoolRole => schoolRole.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(schoolRole => schoolRole.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
