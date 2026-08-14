using LabBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LabBooking.Infrastructure.Sqlserver.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings", t =>
        {
            t.HasCheckConstraint("CK_Bookings_EndTime_After_StartTime", "[EndTime] > [StartTime]");
            t.UseSqlOutputClause(false);
        });
        builder.Property(b => b.Purpose).HasMaxLength(500).IsRequired();

        builder.HasIndex(b => new { b.ResourceId, b.StartTime, b.EndTime });
        builder.HasIndex(b => b.RequesterId);

        builder.HasQueryFilter(b => !b.Requester.IsDeleted && !b.Resource.IsDeleted);

        builder.HasOne(b => b.Resource)
            .WithMany(r => r.Bookings)
            .HasForeignKey(b => b.ResourceId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Requester)
            .WithMany(u => u.RequestedBookings)
            .HasForeignKey(b => b.RequesterId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Rule)
            .WithMany(p => p.Bookings)
            .HasForeignKey(b => b.RuleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.ApprovedByUser)
            .WithMany(u => u.ApprovedBookings)
            .HasForeignKey(b => b.ApprovedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
