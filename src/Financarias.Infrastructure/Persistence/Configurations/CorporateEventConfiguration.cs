using Financarias.Domain.MarketData.CorporateEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Financarias.Infrastructure.Persistence.Configurations;

public sealed class CorporateEventConfiguration : IEntityTypeConfiguration<CorporateEvent>
{
    public void Configure(EntityTypeBuilder<CorporateEvent> builder)
    {
        builder.Property(e => e.IssuerCode)
            .IsRequired()
            .HasMaxLength(4);

        builder.Property(e => e.Isin).HasMaxLength(12);

        builder.Property(e => e.ShareClass)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(e => e.Kind)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(e => e.Amount).HasPrecision(24, 10);
        builder.Property(e => e.Factor).HasPrecision(24, 10);

        builder.Property(e => e.Label)
            .IsRequired()
            .HasMaxLength(40);

        builder.Ignore(e => e.AmountPerShare);

        builder.HasIndex(e => new { e.IssuerCode, e.CumDate });

        builder.ToTable(t => t.HasCheckConstraint(
            "ck_corporate_events_fields_by_kind",
            "(kind IN ('CashDividend', 'InterestOnEquity', 'Yield') " +
            "AND amount IS NOT NULL AND amount >= 0 AND per_shares IS NOT NULL AND per_shares > 0 AND factor IS NULL) " +
            "OR (kind IN ('StockSplit', 'ReverseSplit', 'BonusShares') " +
            "AND factor IS NOT NULL AND factor > 0 AND amount IS NULL AND per_shares IS NULL)"));
    }
}
