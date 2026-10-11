namespace Financarias.Application.MarketData.Stocks.DTOs.Requests;

/// <summary>Um mês de um ano.</summary>
/// <param name="Year">Ano com quatro dígitos, a partir de 1986.</param>
/// <param name="Month">Mês, de 1 a 12.</param>
public sealed record QuoteMonthRequest(int Year, int Month);
