using Financarias.Domain.MarketData.Fuel;
using HotChocolate.Types;

namespace Financarias.Api.GraphQL.Types;

public sealed class FuelStationType
    : ObjectType<FuelStation>
{
    protected override void Configure(IObjectTypeDescriptor<FuelStation> descriptor)
    {
        descriptor.Description("Posto revendedor de combustível.");

        descriptor.Field(s => s.Id).Description("Identificador interno do posto.");
        descriptor.Field(s => s.Cnpj).Description("CNPJ do posto, que o identifica.");
        descriptor.Field(s => s.Name).Description("Nome (razão social) do posto.");
        descriptor.Field(s => s.Brand).Description("Bandeira: a marca sob a qual o posto opera.");
        descriptor.Field(s => s.Region).Description("Macrorregião do país onde o posto fica.");
        descriptor.Field(s => s.State).Description("Sigla da UF.");
        descriptor.Field(s => s.Municipality).Description("Município.");
        descriptor.Field(s => s.Street).Description("Logradouro, quando a fonte informa.");
        descriptor.Field(s => s.Number).Description("Número, quando a fonte informa.");
        descriptor.Field(s => s.Complement).Description("Complemento, quando a fonte informa.");
        descriptor.Field(s => s.Neighborhood).Description("Bairro, quando a fonte informa.");
        descriptor.Field(s => s.PostalCode).Description("CEP, quando a fonte informa.");
    }
}
