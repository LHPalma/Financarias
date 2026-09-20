using Financarias.Domain.MarketData.Fuel;
using HotChocolate.Types;

namespace Financarias.Api.GraphQL.Types;

public sealed class FuelPriceType
    : ObjectType<FuelPrice>
{
    protected override void Configure(IObjectTypeDescriptor<FuelPrice> descriptor)
    {
        descriptor.BindFieldsExplicitly();

        descriptor.Description("Preço de um combustível em um posto, em uma data de coleta.");

        descriptor.Field(p => p.Id).Description("Identificador interno do preço.");
        descriptor.Field(p => p.FuelStation).Description("Posto onde o preço foi coletado.");
        descriptor.Field(p => p.Product).Description("Combustível.");
        descriptor.Field(p => p.CollectedOn).Description("Data da coleta do preço.");
        descriptor.Field(p => p.SalePrice).Description("Preço de venda ao consumidor, na unidade de medida do preço.");
        descriptor.Field(p => p.PurchasePrice)
            .Description("Preço de compra do posto, quando a fonte informa.");
        descriptor.Field(p => p.MeasureUnit)
            .Description("Unidade de medida do preço, como informada pela ANP (por exemplo, R$ / litro).");
    }
}