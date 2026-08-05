using LabBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LabBooking.Infrastructure.Sqlserver.Configurations;

public class WaitlistConfiguration : IEntityTypeConfiguration<Waitlist>
{
    public void Configure(EntityTypeBuilder<Waitlist> builder)
    {
        builder.ToTable("Waitlists", t =>
            t.HasCheckConstraint("CK_Waitlists_DesiredEnd_After_Start", "[DesiredEnd] > [DesiredStart]"));

        builder.HasIndex(w => new { w.ResourceId, w.DesiredStart });

        builder.HasOne<Resource>()
            .WithMany()
            .HasForeignKey(w => w.ResourceId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(w => w.RequesterId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
