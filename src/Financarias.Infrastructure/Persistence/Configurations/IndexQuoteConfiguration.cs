using Financarias.Domain.MarketData.Indexes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Financarias.Infrastructure.Persistence.Configurations;

public sealed class IndexQuoteConfiguration : IEntityTypeConfiguration<IndexQuote>
{
    public void Configure(EntityTypeBuilder<IndexQuote> builder)
    {
        builder.Property(q => q.MarketIndex)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(q => q.Source)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(q => q.CloseValue).HasPrecision(14, 2);

        builder.HasIndex(q => new { q.MarketIndex, q.TradingDate })
            .IsUnique();

        builder.ToTable(t => t.HasCheckConstraint("ck_index_quotes_close_value_positive", "close_value > 0"));
    }
}
