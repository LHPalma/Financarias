using Financarias.Application.MarketData.Stocks.DTOs.Requests;
using HotChocolate.Types;

namespace Financarias.Api.GraphQL.Types;

public sealed class ImportDailyQuotesInputType
    : InputObjectType<ImportDailyQuotesRequest>
{
    protected override void Configure(IInputObjectTypeDescriptor<ImportDailyQuotesRequest> descriptor)
    {
        descriptor.OneOf();
    }
}
