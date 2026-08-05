using LabBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LabBooking.Infrastructure.Sqlserver.Configurations;

public class CheckInOutConfiguration : IEntityTypeConfiguration<CheckInOut>
{
    public void Configure(EntityTypeBuilder<CheckInOut> builder)
    {
        builder.ToTable("CheckInOuts");

        builder.HasIndex(c => c.BookingId).IsUnique();

        builder.HasOne<Booking>()
            .WithMany()
            .HasForeignKey(c => c.BookingId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
