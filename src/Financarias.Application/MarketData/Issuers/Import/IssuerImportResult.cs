namespace Financarias.Application.MarketData.Issuers.Import;

/// <summary>Resultado da importação do cadastro dos emissores.</summary>
/// <param name="IssuersWithQuotes">Emissores distintos que aparecem nas cotações já importadas, pelo prefixo do ISIN. É o conjunto que se tenta cadastrar.</param>
/// <param name="NotListed">Desses, quantos a B3 não lista hoje: empresas que saíram da bolsa, que ficam sem cadastro.</param>
/// <param name="Created">Cadastros novos gravados.</param>
/// <param name="Updated">Cadastros que já existiam e mudaram na B3, atualizados.</param>
public sealed record IssuerImportResult(
    int IssuersWithQuotes,
    int NotListed,
    int Created,
    int Updated);
