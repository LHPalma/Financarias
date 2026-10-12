using Financarias.Domain.MarketData.IndexComposition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Financarias.Infrastructure.Persistence.Configurations;

public sealed class IndexCompositionItemConfiguration : IEntityTypeConfiguration<IndexCompositionItem>
{
    public void Configure(EntityTypeBuilder<IndexCompositionItem> builder)
    {
        builder.Property(i => i.IndexCode)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(i => i.Kind)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(i => i.Ticker)
            .IsRequired()
            .HasMaxLength(8);

        builder.Property(i => i.AssetName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(i => i.ShareType)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(i => i.WeightPercent).HasPrecision(7, IndexCompositionItem.WeightScale);
        builder.Property(i => i.Divisor).HasPrecision(24, IndexCompositionItem.DivisorScale);
        builder.Property(i => i.SourceUrl).HasMaxLength(IndexCompositionItem.MaxSourceUrlLength);

        builder.HasIndex(i => new { i.IndexCode, i.Year, i.Quadrimester, i.Kind, i.Ticker })
            .IsUnique();

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_index_composition_items_quadrimester", "quadrimester BETWEEN 1 AND 3");
            t.HasCheckConstraint("ck_index_composition_items_quantity_positive", "theoretical_quantity > 0");
            t.HasCheckConstraint("ck_index_composition_items_weight_range", "weight_percent BETWEEN 0 AND 100");
        });
    }
}
