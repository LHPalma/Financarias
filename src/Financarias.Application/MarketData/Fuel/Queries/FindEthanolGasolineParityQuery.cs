using Financarias.Application.Common.Messaging;
using Financarias.Application.MarketData.Fuel.DTOs.Results;

namespace Financarias.Application.MarketData.Fuel.Queries;

public sealed record FindEthanolGasolineParityQuery
    : IQuery<IQueryable<EthanolGasolineParityResult>>;