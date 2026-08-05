using LabBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LabBooking.Infrastructure.Sqlserver.Configurations;

public class ResourceConfiguration : IEntityTypeConfiguration<Resource>
{
    public void Configure(EntityTypeBuilder<Resource> builder)
    {
        builder.ToTable("Resources");
        builder.Property(r => r.Name).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Specifications).HasColumnType("nvarchar(max)");
        builder.Property(r => r.ImageUrl).HasMaxLength(500);
        builder.Property(r => r.UsageRules).HasColumnType("nvarchar(max)");

        builder.HasIndex(r => r.DepartmentId);
        builder.HasIndex(r => r.LabManagerId);

        builder.HasOne(r => r.Department)
            .WithMany(d => d.Resources)
            .HasForeignKey(r => r.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.LabManager)
            .WithMany(u => u.ManagedResources)
            .HasForeignKey(r => r.LabManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}
