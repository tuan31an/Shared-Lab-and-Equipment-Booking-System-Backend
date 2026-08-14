using LabBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LabBooking.Infrastructure.Sqlserver.Configurations;

public class ViolationConfiguration : IEntityTypeConfiguration<Violation>
{
    public void Configure(EntityTypeBuilder<Violation> builder)
    {
        builder.ToTable("Violations");
        builder.Property(v => v.Note).HasMaxLength(500);

        builder.HasIndex(v => v.UserId);
        builder.HasIndex(v => new { v.BookingId, v.Type }).IsUnique();

        builder.HasQueryFilter(v => !v.User.IsDeleted);

        builder.HasOne(v => v.User)
            .WithMany(u => u.Violations)
            .HasForeignKey(v => v.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.Booking)
            .WithMany(b => b.Violations)
            .HasForeignKey(v => v.BookingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
