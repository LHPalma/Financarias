using Financarias.Application.Analytics.DTOs.Results;
using Financarias.Application.Common.Messaging;
using Financarias.Domain.Analytics;

namespace Financarias.Application.Analytics.Queries;

public sealed record CalculateNtnbPriceQuery(
    NominalValue VnaBase,
    AnnualYield Yield,
    decimal Inflation,
    DateOnly TradeDate,
    DateOnly DueDate
) : IQuery<NtnbPriceResult>;