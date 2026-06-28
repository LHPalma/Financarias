using Ardalis.Specification.EntityFrameworkCore;
using Financarias.Application.Common.Persistence;
using Financarias.Domain.Common;

namespace Financarias.Infrastructure.Persistence.Repositories;

public class Repository<T>(FinancariasDbContext dbContext)
    : RepositoryBase<T>(dbContext), IRepository<T>
    where T : class, IAggregateRoot
{
}