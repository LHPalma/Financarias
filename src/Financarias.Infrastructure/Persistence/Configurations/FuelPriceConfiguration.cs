using Financarias.Domain.MarketData.Fuel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Financarias.Infrastructure.Persistence.Configurations;

public sealed class FuelPriceConfiguration : IEntityTypeConfiguration<FuelPrice>
{
    public void Configure(EntityTypeBuilder<FuelPrice> builder)
    {
        builder.Property(p => p.Product)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(p => p.SalePrice)
            .IsRequired()
            .HasPrecision(10, 3);

        builder.Property(p => p.PurchasePrice)
            .HasPrecision(10, 3);

        builder.Property(p => p.MeasureUnit)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasOne(p => p.FuelStation)
            .WithMany()
            .HasForeignKey(p => p.StationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.StationId, p.Product, p.CollectedOn })
            .IsUnique();
    }
}
