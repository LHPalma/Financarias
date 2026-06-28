using Ardalis.Specification;
using Financarias.Domain.Common;

namespace Financarias.Application.Common.Persistence;

/// <summary>
/// Repositório de escrita restrito a raízes de agregado (<see cref="IAggregateRoot"/>):
/// herda o contrato CRUD + Specification do Ardalis. Cada agregado resolve sua própria
/// instância fechada.
/// </summary>
public interface IRepository<T> : IRepositoryBase<T>
    where T : class, IAggregateRoot
{
}