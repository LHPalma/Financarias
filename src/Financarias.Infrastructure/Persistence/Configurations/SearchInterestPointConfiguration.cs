using Financarias.Domain.MarketData.SearchInterest;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Financarias.Infrastructure.Persistence.Configurations;

public sealed class SearchInterestPointConfiguration : IEntityTypeConfiguration<SearchInterestPoint>
{
    public void Configure(EntityTypeBuilder<SearchInterestPoint> builder)
    {
        builder.Property(p => p.Geo)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(p => p.ComparisonKey)
            .IsRequired()
            .HasMaxLength(520);

        builder.Property(p => p.Term)
            .IsRequired()
            .HasMaxLength(SearchInterestPoint.MaxTermLength);

        builder.HasIndex(p => new { p.Geo, p.ComparisonKey, p.RangeStart, p.RangeEnd, p.Term, p.Day })
            .IsUnique();

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_search_interest_points_value_range", "value BETWEEN 0 AND 100");
            t.HasCheckConstraint("ck_search_interest_points_day_in_range", "day BETWEEN range_start AND range_end");
        });
    }
}
