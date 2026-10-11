using System.Reflection;
using Financarias.Application.Common.Persistence;
using Financarias.Domain.Holidays.Models;
using Financarias.Domain.MarketData.CorporateEvents;
using Financarias.Domain.Identity;
using Financarias.Domain.MarketData.Fuel;
using Financarias.Domain.MarketData.Indexes;
using Financarias.Domain.MarketData.Inflation;
using Financarias.Domain.MarketData.Indicators;
using Financarias.Domain.MarketData.Stocks;
using Microsoft.EntityFrameworkCore;

namespace Financarias.Infrastructure.Persistence;

public class FinancariasDbContext(DbContextOptions<FinancariasDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Holiday> Holidays => Set<Holiday>();

    public DbSet<FuelStation> FuelStations => Set<FuelStation>();

    public DbSet<FuelPrice> FuelPrices => Set<FuelPrice>();

    public DbSet<DailyQuote> DailyQuotes => Set<DailyQuote>();

    public DbSet<IndexQuote> IndexQuotes => Set<IndexQuote>();

    public DbSet<IndicatorValue> IndicatorValues => Set<IndicatorValue>();

    public DbSet<CorporateEvent> CorporateEvents => Set<CorporateEvent>();

    public DbSet<InflationLevel> InflationLevels => Set<InflationLevel>();

    public DbSet<User> Users => Set<User>();

    IQueryable<Holiday> IApplicationDbContext.Holidays => Holidays;

    IQueryable<FuelStation> IApplicationDbContext.FuelStations => FuelStations;

    IQueryable<FuelPrice> IApplicationDbContext.FuelPrices => FuelPrices;

    IQueryable<User> IApplicationDbContext.Users => Users;

    IQueryable<DailyQuote> IApplicationDbContext.DailyQuotes => DailyQuotes;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}