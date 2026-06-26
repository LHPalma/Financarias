using Financarias.Application.Common.Messaging;
using Financarias.Domain.Addresses;

namespace Financarias.Application.Addresses.Queries;

public sealed record FindAddressByCepQuery(Cep Cep)
    : IQuery<AddressLookupResult?>;
