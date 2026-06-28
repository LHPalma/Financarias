using Financarias.Domain.Holidays.Models;

namespace Financarias.Application.Common.Persistence;

/// <summary>
/// Contrato de leitura sobre o banco: expõe as tabelas como <see cref="IQueryable{T}"/>
/// para os query handlers consultarem via LINQ (sem capacidade de escrita — escrita vai pelo repositório).
/// </summary>
public interface IApplicationDbContext
{
    IQueryable<Holiday> Holidays { get; }
}