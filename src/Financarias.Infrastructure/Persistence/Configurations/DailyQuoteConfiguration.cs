using Financarias.Domain.MarketData.Stocks;
using Financarias.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Financarias.Infrastructure.Persistence.Configurations;

public sealed class DailyQuoteConfiguration : IEntityTypeConfiguration<DailyQuote>
{
    public void Configure(EntityTypeBuilder<DailyQuote> builder)
    {
        builder.Property(q => q.Ticker)
            .IsRequired()
            .HasMaxLength(12)
            .HasConversion<TickerConverter>();

        builder.Property(q => q.MarketType)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(q => q.BdiCode)
            .IsRequired()
            .HasMaxLength(2);

        builder.Property(q => q.CompanyName)
            .IsRequired()
            .HasMaxLength(12);

        builder.Property(q => q.Specification)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(q => q.Isin)
            .IsRequired()
            .HasMaxLength(12);

        builder.Property(q => q.OpenPrice).HasPrecision(13, 2);
        builder.Property(q => q.HighPrice).HasPrecision(13, 2);
        builder.Property(q => q.LowPrice).HasPrecision(13, 2);
        builder.Property(q => q.AveragePrice).HasPrecision(13, 2);
        builder.Property(q => q.ClosePrice).HasPrecision(13, 2);
        builder.Property(q => q.Volume).HasPrecision(18, 2);

        builder.HasIndex(q => new { q.Ticker, q.TradingDate, q.MarketType })
            .IsUnique();

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_daily_quotes_prices_positive",
                "open_price > 0 AND high_price > 0 AND low_price > 0 AND average_price > 0 AND close_price > 0");
            t.HasCheckConstraint("ck_daily_quotes_high_not_below_low", "high_price >= low_price");
            t.HasCheckConstraint(
                "ck_daily_quotes_counts_positive",
                "trades > 0 AND quantity > 0 AND quote_factor > 0");
        });
    }
}
