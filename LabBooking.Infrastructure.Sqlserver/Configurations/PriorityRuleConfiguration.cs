using LabBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LabBooking.Infrastructure.Sqlserver.Configurations;

public class PriorityRuleConfiguration : IEntityTypeConfiguration<PriorityRule>
{
    public void Configure(EntityTypeBuilder<PriorityRule> builder)
    {
        builder.ToTable("PriorityRules");
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(500);
    }
}
