using LabBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LabBooking.Infrastructure.Sqlserver.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings", t =>
            t.HasCheckConstraint("CK_Bookings_EndTime_After_StartTime", "[EndTime] > [StartTime]"));
        builder.Property(b => b.Purpose).HasMaxLength(500).IsRequired();

        builder.HasIndex(b => new { b.ResourceId, b.StartTime, b.EndTime });
        builder.HasIndex(b => b.RequesterId);

        builder.HasOne<Resource>()
            .WithMany()
            .HasForeignKey(b => b.ResourceId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(b => b.RequesterId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PriorityRule>()
            .WithMany()
            .HasForeignKey(b => b.RuleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(b => b.ApprovedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
