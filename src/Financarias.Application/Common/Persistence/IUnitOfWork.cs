namespace Financarias.Application.Common.Persistence;

/// <summary>
/// Sessão de persistência compartilhada pelos repositórios do mesmo escopo: descarta as
/// entidades atualmente rastreadas, deixando o contexto limpo para o próximo lote.
/// </summary>
public interface IUnitOfWork
{
    void ClearTracking();
}
