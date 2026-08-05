using LabBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LabBooking.Infrastructure.Sqlserver.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.Property(n => n.Content).HasColumnType("nvarchar(max)").IsRequired();

        builder.HasIndex(n => n.UserId);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
