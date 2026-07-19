using Financarias.Application.Common.Persistence;

namespace Financarias.Infrastructure.Persistence;

public sealed class UnitOfWork(FinancariasDbContext dbContext) : IUnitOfWork
{
    public void ClearTracking() => dbContext.ChangeTracker.Clear();
}
