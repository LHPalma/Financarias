using Financarias.Application.Analytics.DTOs.Results;
using Financarias.Application.Common.Messaging;
using Financarias.Application.Common.Persistence;
using Financarias.Domain.Analytics.Ntnb;
using Financarias.Domain.Holidays.Models;
using Microsoft.EntityFrameworkCore;

namespace Financarias.Application.Analytics.Queries;

public class CalculateNtnbPriceQueryHandler(
    IApplicationDbContext dbContext
) : IQueryHandler<CalculateNtnbPriceQuery, NtnbPriceResult>
{
    public async Task<NtnbPriceResult> HandleAsync(
        CalculateNtnbPriceQuery query,
        CancellationToken cancellationToken = default)
    {
        var holidays = (await dbContext.Holidays
                .Where(h => h.CountryCode == CountryCode.BR
                            && h.Date >= query.TradeDate
                            && h.Date <= query.DueDate)
                .Select(h => h.Date)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var price = NtnbDatePricing.Calculate(
            vnaBase: query.VnaBase,
            yield: query.Yield,
            projectedInflation: query.Inflation,
            tradeDate: query.TradeDate,
            dueDate: query.DueDate,
            holidays: holidays
        );

        return new NtnbPriceResult(
            SettlementDate: price.SettlementDate,
            ProjectedVna: price.ProjectedVna,
            BusinessDaysToMaturity: price.BusinessDaysToMaturity.Value,
            UnitPrice: price.UnitPrice
        );
    }
}