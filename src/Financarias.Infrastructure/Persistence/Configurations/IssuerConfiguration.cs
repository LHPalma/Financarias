using Financarias.Domain.MarketData.Issuers;
using Financarias.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Financarias.Infrastructure.Persistence.Configurations;

public sealed class IssuerConfiguration : IEntityTypeConfiguration<Issuer>
{
    public void Configure(EntityTypeBuilder<Issuer> builder)
    {
        builder.Property(i => i.IssuerCode)
            .IsRequired()
            .HasMaxLength(4);

        builder.Property(i => i.CvmCode)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(i => i.CompanyName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.TradingName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(i => i.Cnpj)
            .HasMaxLength(14)
            .HasConversion<CnpjConverter>();

        builder.Property(i => i.Sector).HasMaxLength(150);
        builder.Property(i => i.Subsector).HasMaxLength(150);
        builder.Property(i => i.Segment).HasMaxLength(150);
        builder.Property(i => i.ListingMarket).HasMaxLength(60);
        builder.Property(i => i.Website).HasMaxLength(200);

        builder.HasIndex(i => i.IssuerCode)
            .IsUnique();
    }
}
