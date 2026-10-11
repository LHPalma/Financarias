using System.Globalization;
using System.Text;
using ExcelDataReader;
using Financarias.Application.MarketData.Indicators.Import;
using Financarias.Domain.MarketData.Indicators;
using Financarias.Integrations.MarketData.Eia.Clients;
using Microsoft.Extensions.Logging;

namespace Financarias.Integrations.MarketData.Eia.Providers;

public class EiaIndicatorValueProvider(
    IEiaClient client,
    ILogger<EiaIndicatorValueProvider> logger
) : IIndicatorValueProvider
{
    // Planilha da EIA: aba "Data 1", três linhas de cabeçalho, depois Data | Preço
    private const string DataSheet = "Data 1";
    private const int DateColumn = 0;
    private const int PriceColumn = 1;
    private const int StoredDecimals = 6;

    static EiaIndicatorValueProvider()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public async Task<IReadOnlyList<IndicatorValueImportItem>> GetYearAsync(
        EconomicIndicator indicator,
        int year,
        CancellationToken cancellationToken = default)
    {
        await using var download = await client.GetSpotPricesFileAsync(FileFor(indicator), cancellationToken);
        using var buffer = new MemoryStream();
        await download.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        using var reader = ExcelReaderFactory.CreateReader(buffer);

        do
        {
            if (reader.Name == DataSheet)
            {
                return ReadYear(reader, indicator, year);
            }
        }
        while (reader.NextResult());

        logger.LogWarning("EIA workbook for {Indicator} has no '{Sheet}' sheet.", indicator, DataSheet);

        return [];
    }

    private static string FileFor(EconomicIndicator indicator) => indicator switch
    {
        EconomicIndicator.BrentOil => "RBRTEd.xls",
        EconomicIndicator.WtiOil => "RWTCd.xls",
        _ => throw new ArgumentOutOfRangeException(nameof(indicator), indicator, "Not an EIA indicator."),
    };

    private static List<IndicatorValueImportItem> ReadYear(IExcelDataReader reader, EconomicIndicator indicator, int year)
    {
        var items = new List<IndicatorValueImportItem>();

        while (reader.Read())
        {
            if (reader.FieldCount <= PriceColumn
                || reader.GetValue(DateColumn) is not DateTime date
                || date.Year != year
                || reader.GetValue(PriceColumn) is not double price)
            {
                continue;
            }

            var value = Math.Round(Convert.ToDecimal(price, CultureInfo.InvariantCulture), StoredDecimals, MidpointRounding.AwayFromZero);
            items.Add(new IndicatorValueImportItem(indicator, DateOnly.FromDateTime(date), value));
        }

        return items.OrderBy(item => item.ReferenceDate).ToList();
    }
}
