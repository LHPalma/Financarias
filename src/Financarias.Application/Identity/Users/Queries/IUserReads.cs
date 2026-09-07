using Financarias.Domain.Identity;

namespace Financarias.Application.Identity.Users.Queries;

/// <summary>
/// Porta de leitura de usuários: cada método devolve a consulta <b>ainda não executada</b>, para que
/// o Hot Chocolate a componha com <c>[UseProjection]</c>, <c>[UseFiltering]</c> e <c>[UseSorting]</c>
/// e a seleção do GraphQL se resolva numa única consulta SQL, trazendo só as colunas e as linhas
/// que o cliente pediu.
/// <para>
/// Materializar aqui — um <c>ToListAsync</c>, por conveniência — não quebra a compilação nem os
/// testes: quebra o desempenho em silêncio, porque o filtro e a ordenação do cliente passam a rodar
/// em memória, sobre a tabela inteira.
/// </para>
/// <para>
/// Por isso só entra método cuja consulta continue compondo: a tabela aberta, ou um recorte que os
/// argumentos do GraphQL não expressam sozinhos (agrupamento, deduplicação, "o mais recente por X").
/// Leitura que precise calcular um resultado ou materializar antes de responder não mora aqui — vai
/// por caso de uso e handler, devolvendo DTO.
/// </para>
/// </summary>
public interface IUserReads
{
    IQueryable<User> Users();
}
