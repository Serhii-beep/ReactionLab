using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReactionLab.Identity.Contracts;
using ReactionLab.Identity.Persistence.Erasures;

namespace ReactionLab.Identity.Persistence.Configurations;

internal sealed class ErasureConfirmationConfiguration : IEntityTypeConfiguration<ErasureConfirmation>
{
    public void Configure(EntityTypeBuilder<ErasureConfirmation> builder)
    {
        builder.ToTable("erasure_confirmations");
        builder.HasKey(confirmation => new { confirmation.UserId, confirmation.Service });
        builder.Property(confirmation => confirmation.Service).HasMaxLength(ErasureServices.MaximumNameLength);
    }
}
