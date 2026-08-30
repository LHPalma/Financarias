using Financarias.Application.MarketData.Fuel.DTOs.Results;

namespace Financarias.Application.MarketData.Fuel.UseCases;

/// <summary>
/// Compara, por posto, o preço mais recente do etanol com o da gasolina comum e aplica a regra
/// dos 70%: etanol é vantajoso quando custa menos de 70% do preço da gasolina. Postos sem coleta
/// recente de um dos dois produtos ficam de fora do resultado.
/// </summary>
public interface IFindEthanolGasolineParityUseCase
{
    Task<IReadOnlyList<EthanolGasolineParityResult>> ExecuteAsync(CancellationToken cancellationToken = default);
}
