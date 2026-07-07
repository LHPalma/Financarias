using Financarias.Domain.MarketData.Fuel;
using Financarias.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Financarias.Infrastructure.Persistence.Configurations;

public sealed class FuelStationConfiguration : IEntityTypeConfiguration<FuelStation>
{
    public void Configure(EntityTypeBuilder<FuelStation> builder)
    {
        builder.Property(s => s.Cnpj)
            .IsRequired()
            .HasMaxLength(14)
            .HasConversion<CnpjConverter>();

        builder.Property(s => s.Name)
            .IsRequired();

        builder.Property(s => s.Brand)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.Region)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(s => s.State)
            .IsRequired()
            .HasMaxLength(2);

        builder.Property(s => s.Municipality)
            .IsRequired();

        builder.Property(s => s.PostalCode)
            .HasMaxLength(8)
            .HasConversion<CepConverter>();

        builder.HasIndex(s => s.Cnpj)
            .IsUnique();
    }
}
