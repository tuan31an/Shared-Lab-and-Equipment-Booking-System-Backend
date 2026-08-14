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

        builder.HasQueryFilter(w => !w.Requester.IsDeleted && !w.Resource.IsDeleted);

        builder.HasOne(w => w.Resource)
            .WithMany(r => r.Waitlists)
            .HasForeignKey(w => w.ResourceId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(w => w.Requester)
            .WithMany(u => u.Waitlists)
            .HasForeignKey(w => w.RequesterId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
