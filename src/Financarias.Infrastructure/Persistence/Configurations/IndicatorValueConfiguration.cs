using Financarias.Domain.MarketData.Indicators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Financarias.Infrastructure.Persistence.Configurations;

public sealed class IndicatorValueConfiguration : IEntityTypeConfiguration<IndicatorValue>
{
    public void Configure(EntityTypeBuilder<IndicatorValue> builder)
    {
        builder.Property(v => v.Indicator)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(v => v.Value).HasPrecision(18, 6);

        builder.HasIndex(v => new { v.Indicator, v.ReferenceDate })
            .IsUnique();
    }
}
