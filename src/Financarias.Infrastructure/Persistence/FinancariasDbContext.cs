using System.Reflection;
using Financarias.Application.Common.Persistence;
using Financarias.Domain.Holidays.Models;
using Financarias.Domain.Identity;
using Financarias.Domain.MarketData.Fuel;
using Microsoft.EntityFrameworkCore;

namespace Financarias.Infrastructure.Persistence;

public class FinancariasDbContext(DbContextOptions<FinancariasDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Holiday> Holidays => Set<Holiday>();

    public DbSet<FuelStation> FuelStations => Set<FuelStation>();

    public DbSet<FuelPrice> FuelPrices => Set<FuelPrice>();

    public DbSet<User> Users => Set<User>();

    IQueryable<Holiday> IApplicationDbContext.Holidays => Holidays;

    IQueryable<FuelStation> IApplicationDbContext.FuelStations => FuelStations;

    IQueryable<FuelPrice> IApplicationDbContext.FuelPrices => FuelPrices;

    IQueryable<User> IApplicationDbContext.Users => Users;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}