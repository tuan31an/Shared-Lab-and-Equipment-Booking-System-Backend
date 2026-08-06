using LabBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LabBooking.Infrastructure.Sqlserver.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("RefreshTokens");
            builder.Property(rt => rt.Token).HasMaxLength(128).IsRequired();
            builder.Property(rt => rt.ExpiresAtUtc).IsRequired();

            builder.HasIndex(rt => rt.Token).IsUnique();

            builder.HasOne(rt => rt.User)
                .WithMany()
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Khớp query filter của User để tránh cảnh báo EF khi truy vấn qua navigation.
            builder.HasQueryFilter(rt => rt.User == null || !rt.User.IsDeleted);
        }
    }
}