using ActivoosCRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ActivoosCRM.Infrastructure.Persistence.Configurations;

public class ProviderContactConfiguration : IEntityTypeConfiguration<ProviderContact>
{
    public void Configure(EntityTypeBuilder<ProviderContact> builder)
    {
        builder.ToTable("provider_contacts");

        builder.HasKey(pc => pc.Id);

        builder.Property(pc => pc.ContactType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(pc => pc.ContactValue)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(pc => pc.IsPrimary)
            .HasDefaultValue(false);

        // Relationships
        builder.HasOne(pc => pc.Provider)
            .WithMany(p => p.Contacts)
            .HasForeignKey(pc => pc.ProviderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(pc => pc.ProviderId);
        builder.HasIndex(pc => new { pc.ProviderId, pc.ContactType });
    }
}
