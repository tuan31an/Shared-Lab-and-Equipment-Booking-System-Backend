using LabBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LabBooking.Infrastructure.Sqlserver.Configurations;

public class MaintenanceConfiguration : IEntityTypeConfiguration<Maintenance>
{
    public void Configure(EntityTypeBuilder<Maintenance> builder)
    {
        builder.ToTable("Maintenances", t =>
            t.HasCheckConstraint("CK_Maintenances_EndTime_After_StartTime", "[EndTime] > [StartTime]"));
        builder.Property(m => m.Description).HasColumnType("nvarchar(max)");
        builder.Property(m => m.Cost).HasPrecision(12, 2);

        builder.HasIndex(m => m.ResourceId);

        builder.HasOne<Resource>()
            .WithMany()
            .HasForeignKey(m => m.ResourceId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(m => m.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
