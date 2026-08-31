using Financarias.Domain.MarketData.Fuel;
using HotChocolate.Types;

namespace Financarias.Api.GraphQL.Types;

public sealed class FuelPriceType
    : ObjectType<FuelPrice>
{
    protected override void Configure(IObjectTypeDescriptor<FuelPrice> descriptor)
    {
        descriptor.BindFieldsExplicitly();

        descriptor.Field(p => p.Id);
        descriptor.Field(p => p.FuelStation);
        descriptor.Field(p => p.Product);
        descriptor.Field(p => p.CollectedOn);
        descriptor.Field(p => p.SalePrice);
        descriptor.Field(p => p.PurchasePrice);
        descriptor.Field(p => p.MeasureUnit);
    }
}