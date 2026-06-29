using Financarias.Application.Analytics.DTOs.Requests;
using Financarias.Application.Analytics.DTOs.Results;

namespace Financarias.Application.Analytics.UseCases;

/// <summary>
/// Precifica a NTN-B Principal por datas a partir do <see cref="CalculateNtnbPriceRequest"/>:
/// valida os campos em VOs, carrega os feriados (BR) do intervalo e calcula liquidação T+2,
/// VNA projetado, dias úteis até o vencimento e o PU. Taxas em fração (0.06 = 6%).
/// </summary>
public interface ICalculateNtnbPriceUseCase
{
    Task<NtnbPriceResult> ExecuteAsync(
        CalculateNtnbPriceRequest request,
        CancellationToken cancellationToken = default);
}
