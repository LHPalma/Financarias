namespace Financarias.Application.MarketData.Indexes.Import;

/// <summary>Resultado da importação dos fechamentos diários de um índice.</summary>
/// <param name="RowsRead">Pregões com fechamento válido lidos da B3 e processados. Zero para um ano sem dados, como um ano futuro.</param>
/// <param name="QuotesCreated">Fechamentos novos inseridos.</param>
/// <param name="QuotesUpdated">Fechamentos já existentes (mesmo índice e pregão) cujo valor mudou e foi atualizado.</param>
public sealed record IndexQuoteImportResult(
    int RowsRead,
    int QuotesCreated,
    int QuotesUpdated);
