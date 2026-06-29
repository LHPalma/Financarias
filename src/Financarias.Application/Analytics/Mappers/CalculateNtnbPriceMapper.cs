using Financarias.Application.Analytics.DTOs.Requests;
using Financarias.Application.Analytics.Queries;
using Financarias.Domain.Analytics;
using Riok.Mapperly.Abstractions;

namespace Financarias.Application.Analytics.Mappers;

[Mapper]
public partial class CalculateNtnbPriceMapper
{
    public partial CalculateNtnbPriceQuery ToQuery(CalculateNtnbPriceRequest request);

    private static NominalValue ToNominalValue(decimal value) => NominalValue.Create(value);

    private static AnnualYield ToAnnualYield(decimal value) => AnnualYield.FromFraction(value);
}
