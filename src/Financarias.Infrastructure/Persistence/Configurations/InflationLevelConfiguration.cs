using Financarias.Domain.MarketData.Inflation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Financarias.Infrastructure.Persistence.Configurations;

public sealed class InflationLevelConfiguration : IEntityTypeConfiguration<InflationLevel>
{
    public void Configure(EntityTypeBuilder<InflationLevel> builder)
    {
        builder.Property(l => l.Level).HasPrecision(38, 16);

        builder.HasIndex(l => l.Day)
            .IsUnique();

        builder.ToTable(t => t.HasCheckConstraint("ck_inflation_levels_level_positive", "level > 0"));
    }
}
