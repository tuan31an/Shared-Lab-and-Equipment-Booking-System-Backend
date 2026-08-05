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

        builder.HasQueryFilter(c => !c.Booking.Requester.IsDeleted && !c.Booking.Resource.IsDeleted);

        builder.HasOne(c => c.Booking)
            .WithOne(b => b.CheckInOut)
            .HasForeignKey<CheckInOut>(c => c.BookingId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
