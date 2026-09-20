namespace Financarias.Application.Analytics.DTOs.Requests;

/// <summary>Dados para precificar uma NTN-B Principal por datas.</summary>
/// <param name="VnaBase">VNA (valor nominal atualizado) no dia 15 âncora, isto é, o último VNA publicado, em reais. Precisa ser positivo.</param>
/// <param name="Yield">Taxa interna de retorno anual, em fração (0.06 = 6% a.a.). Precisa ser maior que -1.</param>
/// <param name="Inflation">Projeção do IPCA do mês de referência, em fração (0.005 = 0,5%).</param>
/// <param name="TradeDate">Data de negociação, que é a data de cálculo.</param>
/// <param name="DueDate">Data de vencimento do título.</param>
public sealed record CalculateNtnbPriceRequest(
    decimal VnaBase,
    decimal Yield,
    decimal Inflation,
    DateOnly TradeDate,
    DateOnly DueDate);
