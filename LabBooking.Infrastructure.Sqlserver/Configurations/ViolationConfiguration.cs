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

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(v => v.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Booking>()
            .WithMany()
            .HasForeignKey(v => v.BookingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
