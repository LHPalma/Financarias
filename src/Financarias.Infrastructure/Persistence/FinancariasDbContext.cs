using System.Reflection;
using Financarias.Application.Common.Persistence;
using Financarias.Domain.Holidays.Models;
using Microsoft.EntityFrameworkCore;

namespace Financarias.Infrastructure.Persistence;

public class FinancariasDbContext(DbContextOptions<FinancariasDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Holiday> Holidays => Set<Holiday>();

    IQueryable<Holiday> IApplicationDbContext.Holidays => Holidays;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}