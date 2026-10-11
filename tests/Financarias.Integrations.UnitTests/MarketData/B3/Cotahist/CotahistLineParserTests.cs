using Financarias.Domain.MarketData.Currencies;
using Financarias.Domain.MarketData.Stocks;
using Financarias.Integrations.MarketData.B3.Cotahist;

namespace Financarias.Integrations.UnitTests.MarketData.B3.Cotahist;

public class CotahistLineParserTests
{
    // Linhas reais do COTAHIST_D09102026 (registro 01, 245 caracteres).
    private const string Petr4Line =
        "012026100902PETR4       010PETROBRAS   PN      N2   R$  000000000554500000000056800000000005510000000000560700000000056000000000005600000000000560291623000000000049809400000000279303989300000000000000009999123100000010000000000000BRPETRACNPR6230";

    private const string B3sa3Line =
        "012026100902B3SA3       010B3          ON      NM   R$  000000000230000000000023530000000002298000000000232900000000023350000000002334000000000233565132000000000063980100000000149070868200000000000000009999123100000010000000000000BRB3SAACNOR6143";

    private const string Aapl34Line =
        "012026100934AAPL34      010APPLE       DRN          R$  000000000829800000000084370000000008266000000000833700000000083650000000008360000000000840202007000000000000385812000000003216535335000000000000009999123100000010000000000000BRAAPLBDR004160";

    private const string Bova11Line =
        "012026100914BOVA11      010ISHARES BOVACI           R$  000000002047500000000206480000000020415000000002055800000000205550000000020555000000002056192549000000000007448195000000153123463121000000000000009999123100000010000000000000BRBOVACTF003120";

    private const string Alup3fLine =
        "012026100996ALUP3F      020ALUPAR      ON      N2   R$  000000000140900000000014800000000001388000000000141300000000014070000000001407000000000141800066000000000000000805000000000001138152000000000000009999123100000010000000000000BRALUPACNOR8129";

    private const string Fnam11Line =
        "012026100914FNAM11      010FINAM       CI *         R$  000000000004000000000000420000000000039000000000004100000000000400000000000040000000000004100179000000000073329000000000000003041854000000000000009999123100010000000000000000BRFNAMCTF013002";

    private const string Ibov11Line =
        "012026100902IBOV11      010IBOVESPA    IBO/         R$  000002088180000000208818000000020881800000002088180000000208818000000000000000000000000000000029000000000000073601000000015369213618000000000000009999123100001000000000000000BRIBOVINDM18021";

    private const string Rzzr11Line =
        "012026100912RZZR11      010FII RZZR    CI           R$  000000001344000000000134400000000013440000000001344600000000134400000000013251000000001345800002000000000000000056000000000000752982000000000000009999123100000010000000000000BRRZZRCTF000125";

    private const string Mrsa3bfLine =
        "012026100996MRSA3BF     020MRS LOGIST  ON      MB   R$  000000000415300000000042350000000004153000000000419400000000042350000000004006000000000423500004000000000000000010000000000000041940000000000000009999123100000010000000000000BRMRSAACNOR6127";

    // Linhas reais do COTAHIST_A1995: códigos de três letras com espaço de preenchimento.
    private const string LegacyPetr4Line =
        "011995060102PET 4       010PETROBRAS   PN *         R$  000000000850000000000089000000000008450000000000868600000000089000000000008800000000000885000468000000000157050000000000001364252430000000000000009999123100010000000000000000BRPETRACNPR6134";

    private const string LegacyBbas4Line =
        "011995060102BB  4       010BRASIL      PN *         R$  000000000107000000000011000000000001065000000000107600000000010800000000001065000000000108000054000000000025590000000000000027544820000000000000009999123100010000000000000000BRBBASACNPR0156";

    private const string LegacyAces3OldIsinLine =
        "011995010202ACE 3       010ACESITA     ON *INT      R$  000000000630000000000063000000000006300000000000630000000000063000000000006300000000000650000001000000000000200000000000000001260000000000000000009999123100010000000000000000ACESACON    119";

    // Linhas reais dos anuais de 1986 a 1994: a mesma ACESITA em cinco moedas diferentes.
    private const string Cruzeiro1986Line =
        "011986010202ACE 2       010ACESITA     PP  C03      CR$ 000000000043000000000004300000000000430000000000043000000000004300000000000440000000000000000002000000000001200000000000000516000000000000000000009999123100000010000000000000ACESACPP    113";

    private const string Cruzado1986Line =
        "011986030402ACE 2       010ACESITA     PP *C03      CZ$ 000000000060000000000006500000000000500000000000061700000000006000000000000000000000000000000013000000000058480000000000000036138000000000000000009999123100010000000000000000ACESACPP    113";

    private const string CruzadoNovo1990Line =
        "011990010202EAL 1       010ACO ALTONA  OP *         NCZ$000000000300000000000030000000000003000000000000300000000000030000000000003000000000000000000002000000000000177500000000000000532500000000000000009999123100010000000000000000EALTACOP    121";

    private const string Cruzeiro1990Line =
        "011990031902PMA 2       010PARANAPANEMAPP *ES       CR$ 000000004200000000000420000000000040000000000004068200000000400000000000040000000000000000000008000000000000170000000000000006916000000000000000009999123100010000000000000000PMAMACPP    017";

    private const string Cruzeiro1993Line =
        "011993010402ACE 4       010ACESITA     PN           CR$ 000000020200000000002020000000000202000000000020200000000002020000000000220000000000000000000001000000000000001000000000000202000000000000000000009999123100000010000000000000ACESACPN    115";

    private const string CruzeiroReal1994Line =
        "011994030802ACE 3       010ACESITA     ON  INT      CR$ 000000003300000000000330000000000031000000000003152000000000320000000000031000000000003240000012000000000000250000000000007880000000000000000000009999123100000010000000000000ACESACON    116";

    // Linha real do COTAHIST_A2000: mercado fracionário com sufixo duplo.
    private const string DoubleFractionalSuffixLine =
        "012000010496PETR4FF     020PETROBRAS   PN *         R$  000000004690000000000469000000000044400000000004546000000000446400000000043000000000004420000062000000000000132176000000000006007875000000000000009999123100010000000000000000BRPETRACNPR6140";

    private const string OptionLine =
        "012026100978ABCBJ239    070ABCB        PN      N2000R$  000000000052000000000005200000000000520000000000052000000000005200000000000000000000000000000002000000000000003300000000000001716000000000000239002026101600000010000000000000BRABCBACNPR4151";

    private const string HeaderLine =
        "00COTAHIST.2026BOVESPA 20261009                                                                                                                                                                                                                      ";

    private const string TrailerLine =
        "99COTAHIST.2026BOVESPA 2026100900000019213                                                                                                                                                                                                           ";

    public static TheoryData<string, string> MalformedLines => new()
    {
        { "comprimento menor que 245", Petr4Line[..244] },
        { "comprimento maior que 245", Petr4Line + " " },
        { "data impossível", Replace(Petr4Line, 3, "20261340") },
        { "ticker em branco", Replace(Petr4Line, 13, "            ") },
        { "ticker fora do padrão", Mrsa3bfLine },
        { "moeda diferente de R$", Replace(Petr4Line, 53, "US$ ") },
        { "preço com caractere não numérico", Replace(Petr4Line, 57, "00000000055A5") },
        { "preço de abertura zero", Replace(Petr4Line, 57, "0000000000000") },
        { "máxima abaixo da mínima", Replace(Petr4Line, 70, "0000000005000") },
        { "zero negócios", Replace(Petr4Line, 148, "00000") },
        { "quantidade com caractere não numérico", Replace(Petr4Line, 153, "00000000000049X094") },
        { "fator de cotação zero", Replace(Petr4Line, 211, "0000000") },
    };

    public static TheoryData<string, string> OutOfScopeLines => new()
    {
        { "mercado de opções de compra", OptionLine },
        { "registro header (00)", HeaderLine },
        { "registro trailer (99)", TrailerLine },
        { "linha em branco", string.Empty },
    };

    [Fact(DisplayName = "TryParse lê uma ação comum com todos os campos, preços divididos por 100")]
    public void TryParse_ReturnsParsed_ForCommonShare()
    {
        // Act
        var result = CotahistLineParser.TryParse(Petr4Line, out var item);

        // Assert
        Assert.Equal(CotahistLineResult.Parsed, result);
        Assert.NotNull(item);
        Assert.Equal("PETR4", item.Ticker.Value);
        Assert.Equal(new DateOnly(2026, 10, 9), item.TradingDate);
        Assert.Equal(QuoteMarketType.Cash, item.MarketType);
        Assert.Equal("02", item.BdiCode);
        Assert.Equal("PETROBRAS", item.CompanyName);
        Assert.Equal("PN      N2", item.Specification);
        Assert.Equal("BRPETRACNPR6", item.Isin);
        Assert.Equal(55.45m, item.OpenPrice);
        Assert.Equal(56.80m, item.HighPrice);
        Assert.Equal(55.10m, item.LowPrice);
        Assert.Equal(56.07m, item.AveragePrice);
        Assert.Equal(56.00m, item.ClosePrice);
        Assert.Equal(91_623, item.Trades);
        Assert.Equal(49_809_400, item.Quantity);
        Assert.Equal(2_793_039_893.00m, item.Volume);
        Assert.Equal(1, item.QuoteFactor);
    }

    [Fact(DisplayName = "TryParse aceita ticker com dígito na raiz (B3SA3)")]
    public void TryParse_ReturnsParsed_ForTickerWithDigitInRoot()
    {
        // Act
        var result = CotahistLineParser.TryParse(B3sa3Line, out var item);

        // Assert
        Assert.Equal(CotahistLineResult.Parsed, result);
        Assert.Equal("B3SA3", item!.Ticker.Value);
        Assert.Equal("B3", item.CompanyName);
    }

    [Fact(DisplayName = "TryParse lê um BDR (BDI 34, espécie DRN)")]
    public void TryParse_ReturnsParsed_ForBdr()
    {
        // Act
        var result = CotahistLineParser.TryParse(Aapl34Line, out var item);

        // Assert
        Assert.Equal(CotahistLineResult.Parsed, result);
        Assert.Equal("AAPL34", item!.Ticker.Value);
        Assert.Equal("34", item.BdiCode);
        Assert.Equal("DRN", item.Specification);
        Assert.Equal(32_165_353.35m, item.Volume);
    }

    [Fact(DisplayName = "TryParse lê um ETF com espécie curta e nome de 12 caracteres")]
    public void TryParse_ReturnsParsed_ForEtf()
    {
        // Act
        var result = CotahistLineParser.TryParse(Bova11Line, out var item);

        // Assert
        Assert.Equal(CotahistLineResult.Parsed, result);
        Assert.Equal("BOVA11", item!.Ticker.Value);
        Assert.Equal("ISHARES BOVA", item.CompanyName);
        Assert.Equal("CI", item.Specification);
        Assert.Equal(204.75m, item.OpenPrice);
    }

    [Fact(DisplayName = "TryParse lê o mercado fracionário (TPMERC 020, BDI 96)")]
    public void TryParse_ReturnsParsed_ForFractionalMarket()
    {
        // Act
        var result = CotahistLineParser.TryParse(Alup3fLine, out var item);

        // Assert
        Assert.Equal(CotahistLineResult.Parsed, result);
        Assert.Equal("ALUP3F", item!.Ticker.Value);
        Assert.Equal(QuoteMarketType.Fractional, item.MarketType);
        Assert.Equal("96", item.BdiCode);
    }

    [Fact(DisplayName = "TryParse converte código antigo de três letras (PET 4) pela raiz do ISIN e guarda o original")]
    public void TryParse_DerivesTickerFromIsinRoot_ForLegacyCode()
    {
        // Act
        var result = CotahistLineParser.TryParse(LegacyPetr4Line, out var item);

        // Assert
        Assert.Equal(CotahistLineResult.Parsed, result);
        Assert.Equal("PETR4", item!.Ticker.Value);
        Assert.Equal("PET 4", item.LegacyTicker);
    }

    [Fact(DisplayName = "TryParse converte código antigo de duas letras com preenchimento duplo (BB  4)")]
    public void TryParse_DerivesTickerFromIsinRoot_ForTwoLetterLegacyCode()
    {
        // Act
        var result = CotahistLineParser.TryParse(LegacyBbas4Line, out var item);

        // Assert
        Assert.Equal(CotahistLineResult.Parsed, result);
        Assert.Equal("BBAS4", item!.Ticker.Value);
        Assert.Equal("BB  4", item.LegacyTicker);
    }

    [Fact(DisplayName = "TryParse deriva a raiz das quatro primeiras posições quando o campo do ISIN tem o código antigo da empresa")]
    public void TryParse_DerivesTickerFromCompanyCode_WhenIsinFieldHasNoBrPrefix()
    {
        // Act
        var result = CotahistLineParser.TryParse(LegacyAces3OldIsinLine, out var item);

        // Assert
        Assert.Equal(CotahistLineResult.Parsed, result);
        Assert.Equal("ACES3", item!.Ticker.Value);
        Assert.Equal("ACE 3", item.LegacyTicker);
    }

    [Fact(DisplayName = "TryParse reduz o sufixo duplo do fracionário (PETR4FF) a PETR4F e guarda o original")]
    public void TryParse_CollapsesDoubleFractionalSuffix()
    {
        // Act
        var result = CotahistLineParser.TryParse(DoubleFractionalSuffixLine, out var item);

        // Assert
        Assert.Equal(CotahistLineResult.Parsed, result);
        Assert.Equal("PETR4F", item!.Ticker.Value);
        Assert.Equal("PETR4FF", item.LegacyTicker);
        Assert.Equal(QuoteMarketType.Fractional, item.MarketType);
    }

    [Theory(DisplayName = "TryParse lê as linhas das moedas antigas guardando o preço na moeda original")]
    [InlineData(nameof(Cruzeiro1986Line), "ACES2", "1986-01-02", 4.30)]
    [InlineData(nameof(Cruzado1986Line), "ACES2", "1986-03-04", 6.00)]
    [InlineData(nameof(CruzadoNovo1990Line), "EALT1", "1990-01-02", 30.00)]
    [InlineData(nameof(Cruzeiro1990Line), "PMAM2", "1990-03-19", 420.00)]
    [InlineData(nameof(Cruzeiro1993Line), "ACES4", "1993-01-04", 2020.00)]
    [InlineData(nameof(CruzeiroReal1994Line), "ACES3", "1994-03-08", 330.00)]
    public void TryParse_ReadsOldCurrencyLines_KeepingTheOriginalPrice(
        string lineName, string ticker, string date, double openPrice)
    {
        // Arrange
        var line = LineByName(lineName);

        // Act
        var result = CotahistLineParser.TryParse(line, out var item);

        // Assert
        Assert.Equal(CotahistLineResult.Parsed, result);
        Assert.Equal(ticker, item!.Ticker.Value);
        Assert.Equal(DateOnly.Parse(date), item.TradingDate);
        Assert.Equal((decimal)openPrice, item.OpenPrice);
        Assert.NotNull(item.LegacyTicker);
    }

    [Fact(DisplayName = "O preço lido em Cruzeiro vira o equivalente exato em reais pelo conversor do domínio")]
    public void TryParse_ThenToReais_GivesTheExactEquivalentInReais()
    {
        // Act
        CotahistLineParser.TryParse(Cruzeiro1993Line, out var item);
        var inReais = BrazilianCurrency.ToReais(item!.OpenPrice, item.TradingDate);

        // Assert
        Assert.Equal(2_020m / 2_750_000m, inReais);
    }

    [Theory(DisplayName = "TryParse pula a linha cujo código de moeda não é o da moeda que vigorava na data")]
    [InlineData(nameof(Cruzeiro1993Line), 53, "R$  ")]
    [InlineData(nameof(Cruzeiro1986Line), 53, "CZ$ ")]
    [InlineData(nameof(Cruzado1986Line), 53, "CR$ ")]
    [InlineData(nameof(CruzadoNovo1990Line), 53, "CR$ ")]
    [InlineData(nameof(Cruzeiro1993Line), 53, "US$ ")]
    public void TryParse_ReturnsSkipped_WhenTheCurrencyCodeDoesNotMatchTheDate(string lineName, int position, string value)
    {
        // Arrange
        var line = Replace(LineByName(lineName), position, value);

        // Act
        var result = CotahistLineParser.TryParse(line, out var item);

        // Assert
        Assert.Equal(CotahistLineResult.Skipped, result);
        Assert.Null(item);
    }

    [Fact(DisplayName = "TryParse pula uma linha em Cruzeiro Real datada depois do Real (CR$ em 2026)")]
    public void TryParse_ReturnsSkipped_ForOldCurrencyCodeInTheRealEra()
    {
        // Arrange
        var line = Replace(Petr4Line, 53, "CR$ ");

        // Act
        var result = CotahistLineParser.TryParse(line, out var item);

        // Assert
        Assert.Equal(CotahistLineResult.Skipped, result);
        Assert.Null(item);
    }

    [Fact(DisplayName = "TryParse pula o código antigo quando o campo do ISIN não permite derivar a raiz")]
    public void TryParse_ReturnsSkipped_WhenLegacyCodeHasNoUsableIsin()
    {
        // Arrange
        var line = Replace(LegacyPetr4Line, 231, "            ");

        // Act
        var result = CotahistLineParser.TryParse(line, out var item);

        // Assert
        Assert.Equal(CotahistLineResult.Skipped, result);
        Assert.Null(item);
    }

    [Fact(DisplayName = "TryParse deixa o código antigo vazio para ticker moderno")]
    public void TryParse_LeavesLegacyTickerNull_ForModernCode()
    {
        // Act
        CotahistLineParser.TryParse(Petr4Line, out var item);

        // Assert
        Assert.Null(item!.LegacyTicker);
    }

    [Fact(DisplayName = "TryParse preserva o fator de cotação 1000 do arquivo")]
    public void TryParse_PreservesQuoteFactorOfOneThousand()
    {
        // Act
        var result = CotahistLineParser.TryParse(Fnam11Line, out var item);

        // Assert
        Assert.Equal(CotahistLineResult.Parsed, result);
        Assert.Equal(1000, item!.QuoteFactor);
    }

    [Fact(DisplayName = "TryParse preserva o fator de cotação 100, que o PDF da B3 não documenta")]
    public void TryParse_PreservesQuoteFactorOfOneHundred()
    {
        // Act
        var result = CotahistLineParser.TryParse(Ibov11Line, out var item);

        // Assert
        Assert.Equal(CotahistLineResult.Parsed, result);
        Assert.Equal(100, item!.QuoteFactor);
    }

    [Fact(DisplayName = "TryParse aceita preço médio fora do intervalo mínima–máxima (acontece no arquivo real)")]
    public void TryParse_ReturnsParsed_WhenAverageIsOutsideRange()
    {
        // Act
        var result = CotahistLineParser.TryParse(Rzzr11Line, out var item);

        // Assert
        Assert.Equal(CotahistLineResult.Parsed, result);
        Assert.Equal(134.46m, item!.AveragePrice);
        Assert.Equal(134.40m, item.HighPrice);
    }

    [Theory(DisplayName = "TryParse ignora sem contar como pulada o que está fora de escopo")]
    [MemberData(nameof(OutOfScopeLines))]
    public void TryParse_ReturnsOutOfScope_ForLinesOutsideTheScope(string description, string line)
    {
        // Act
        var result = CotahistLineParser.TryParse(line, out var item);

        // Assert
        Assert.True(result == CotahistLineResult.OutOfScope, description);
        Assert.Null(item);
    }

    [Theory(DisplayName = "TryParse pula a linha de escopo malformada")]
    [MemberData(nameof(MalformedLines))]
    public void TryParse_ReturnsSkipped_ForMalformedQuoteLines(string description, string line)
    {
        // Act
        var result = CotahistLineParser.TryParse(line, out var item);

        // Assert
        Assert.True(result == CotahistLineResult.Skipped, description);
        Assert.Null(item);
    }

    private static string LineByName(string name) => name switch
    {
        nameof(Cruzeiro1986Line) => Cruzeiro1986Line,
        nameof(Cruzado1986Line) => Cruzado1986Line,
        nameof(CruzadoNovo1990Line) => CruzadoNovo1990Line,
        nameof(Cruzeiro1990Line) => Cruzeiro1990Line,
        nameof(Cruzeiro1993Line) => Cruzeiro1993Line,
        nameof(CruzeiroReal1994Line) => CruzeiroReal1994Line,
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, null),
    };

    private static string Replace(string line, int firstPosition, string value) =>
        line[..(firstPosition - 1)] + value + line[(firstPosition - 1 + value.Length)..];
}
