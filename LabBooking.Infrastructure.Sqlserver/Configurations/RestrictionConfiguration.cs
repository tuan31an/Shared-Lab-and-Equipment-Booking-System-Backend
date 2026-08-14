using LabBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LabBooking.Infrastructure.Sqlserver.Configurations;

public class RestrictionConfiguration : IEntityTypeConfiguration<Restriction>
{
    public void Configure(EntityTypeBuilder<Restriction> builder)
    {
        builder.ToTable("Restrictions", t =>
            t.HasCheckConstraint("CK_Restrictions_EndDate_After_StartDate", "[EndDate] >= [StartDate]"));
        builder.Property(r => r.Reason).HasMaxLength(500).IsRequired();
        builder.Property(r => r.StartDate).HasColumnType("date");
        builder.Property(r => r.EndDate).HasColumnType("date");

        builder.HasIndex(r => r.UserId);

        builder.HasQueryFilter(r => !r.User.IsDeleted);

        builder.HasOne(r => r.User)
            .WithMany(u => u.Restrictions)
            .HasForeignKey(r => r.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.CreatedByUser)
            .WithMany()
            .HasForeignKey(r => r.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
