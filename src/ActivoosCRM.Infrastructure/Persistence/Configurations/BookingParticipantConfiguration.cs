using ActivoosCRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ActivoosCRM.Infrastructure.Persistence.Configurations;

public class BookingParticipantConfiguration : IEntityTypeConfiguration<BookingParticipant>
{
    public void Configure(EntityTypeBuilder<BookingParticipant> builder)
    {
        builder.ToTable("booking_participants");

        builder.HasKey(bp => bp.Id);

        builder.Property(bp => bp.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(bp => bp.Gender)
            .HasMaxLength(20);

        builder.Property(bp => bp.ContactPhone)
            .HasMaxLength(20);

        builder.Property(bp => bp.DietaryRestrictions)
            .HasColumnType("text");

        builder.Property(bp => bp.MedicalConditions)
            .HasColumnType("text");

        // Relationships
        builder.HasOne(bp => bp.Booking)
            .WithMany(b => b.Participants)
            .HasForeignKey(bp => bp.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(bp => bp.BookingId);
    }
}
