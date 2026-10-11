using Financarias.Domain.MarketData;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Financarias.Infrastructure.Persistence.Converters;

public sealed class TickerConverter() : ValueConverter<Ticker, string>(
    ticker => ticker.Value,
    value => Ticker.Create(value));
