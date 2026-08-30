using System.Reflection;
using Financarias.Application.Common.Persistence;
using Financarias.Domain.Holidays.Models;
using Financarias.Domain.MarketData.Fuel;
using Microsoft.EntityFrameworkCore;

namespace Financarias.Infrastructure.Persistence;

public class FinancariasDbContext(DbContextOptions<FinancariasDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Holiday> Holidays => Set<Holiday>();

    public DbSet<FuelStation> FuelStations => Set<FuelStation>();

    public DbSet<FuelPrice> FuelPrices => Set<FuelPrice>();

    IQueryable<Holiday> IApplicationDbContext.Holidays => Holidays;

    IQueryable<FuelStation> IApplicationDbContext.FuelStations => FuelStations;

    IQueryable<FuelPrice> IApplicationDbContext.FuelPrices => FuelPrices;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}