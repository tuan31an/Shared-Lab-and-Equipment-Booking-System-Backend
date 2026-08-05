using LabBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LabBooking.Infrastructure.Sqlserver.Configurations;

public class IncidentConfiguration : IEntityTypeConfiguration<Incident>
{
    public void Configure(EntityTypeBuilder<Incident> builder)
    {
        builder.ToTable("Incidents");
        builder.Property(i => i.Description).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(i => i.ImageUrl).HasMaxLength(500);

        builder.HasIndex(i => i.ResourceId);

        builder.HasOne<Booking>()
            .WithMany()
            .HasForeignKey(i => i.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Resource>()
            .WithMany()
            .HasForeignKey(i => i.ResourceId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(i => i.ReportedBy)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
