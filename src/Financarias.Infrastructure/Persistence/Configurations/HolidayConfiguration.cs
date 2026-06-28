using Financarias.Domain.Holidays;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Financarias.Infrastructure.Persistence.Configurations;

public sealed class HolidayConfiguration : IEntityTypeConfiguration<Holiday>
{
    public void Configure(EntityTypeBuilder<Holiday> builder)
    {
        builder.Property(h => h.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(h => h.CountryCode)
            .IsRequired()
            .HasMaxLength(2)
            .HasConversion<string>();

        builder.HasIndex(h => new { h.Date, h.CountryCode })
            .IsUnique();
    }
}