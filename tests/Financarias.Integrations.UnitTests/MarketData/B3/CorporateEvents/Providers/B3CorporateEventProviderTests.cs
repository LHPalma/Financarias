using System.Net;
using System.Text;
using System.Text.Json;
using Financarias.Domain.MarketData.CorporateEvents;
using Financarias.Integrations.MarketData.B3.CorporateEvents.Clients;
using Financarias.Integrations.MarketData.B3.CorporateEvents.DTOs.Responses;
using Financarias.Integrations.MarketData.B3.CorporateEvents.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Refit;

namespace Financarias.Integrations.UnitTests.MarketData.B3.CorporateEvents.Providers;

public class B3CorporateEventProviderTests
{
    private readonly IB3CompanyClient _client = Substitute.For<IB3CompanyClient>();
    private readonly B3CorporateEventProvider _provider;

    public B3CorporateEventProviderTests()
    {
        _provider = new B3CorporateEventProvider(_client, NullLogger<B3CorporateEventProvider>.Instance);
        SupplementReturns("[]");
    }

    [Fact(DisplayName = "Mapeia dividendo, JCP e rendimento com espécie, lote, datas e valor em pt-BR")]
    public async Task Get_MapsTheThreeCashKinds()
    {
        // Arrange
        CashReturns(
            """
            { "page": { "pageNumber": 1, "pageSize": 100, "totalRecords": 3, "totalPages": 1 },
              "results": [
                { "typeStock": "ON", "dateApproval": "06/08/2026", "valueCash": "0,67407131", "ratio": "1",
                  "corporateAction": "JRS CAP PROPRIO", "lastDatePriorEx": "21/08/2026" },
                { "typeStock": "PN", "dateApproval": "25/04/2024", "valueCash": "0,55015787", "ratio": "1",
                  "corporateAction": "DIVIDENDO", "lastDatePriorEx": "25/04/2024" },
                { "typeStock": "ON", "dateApproval": "25/04/2024", "valueCash": "0,02237057", "ratio": "1",
                  "corporateAction": "RENDIMENTO", "lastDatePriorEx": "25/04/2024" }
              ] }
            """);

        // Act
        var fetch = await _provider.GetAsync("PETR", ["PETROBRAS"]);

        // Assert
        Assert.Equal(0, fetch.Skipped);
        Assert.Equal(3, fetch.Items.Count);

        var interest = fetch.Items[0];
        Assert.Equal(CorporateEventKind.InterestOnEquity, interest.Kind);
        Assert.Equal("ON", interest.ShareClass);
        Assert.Equal(new DateOnly(2026, 8, 21), interest.CumDate);
        Assert.Equal(new DateOnly(2026, 8, 6), interest.ApprovalDate);
        Assert.Equal(0.67407131m, interest.Amount);
        Assert.Equal(1, interest.PerShares);
        Assert.Null(interest.Isin);
        Assert.Null(interest.PaymentDate);
        Assert.Null(interest.Factor);
        Assert.Equal("JRS CAP PROPRIO", interest.Label);

        Assert.Equal(CorporateEventKind.CashDividend, fetch.Items[1].Kind);
        Assert.Equal(CorporateEventKind.Yield, fetch.Items[2].Kind);
    }

    [Fact(DisplayName = "Lê o lote de mil ações dos proventos antigos e a data de aprovação sentinela vira nula")]
    public async Task Get_ReadsTheOldQuoteLotAndTheSentinelDate()
    {
        // Arrange
        CashReturns(
            """
            { "page": { "totalPages": 1 },
              "results": [ { "typeStock": "PN", "dateApproval": "01/01/1900", "valueCash": "5,1471", "ratio": "1000",
                             "corporateAction": "DIVIDENDO", "lastDatePriorEx": "21/03/1996" } ] }
            """);

        // Act
        var fetch = await _provider.GetAsync("PETR", ["PETROBRAS"]);

        // Assert
        var item = Assert.Single(fetch.Items);
        Assert.Equal(1000, item.PerShares);
        Assert.Equal(5.1471m, item.Amount);
        Assert.Null(item.ApprovalDate);
    }

    [Fact(DisplayName = "Percorre todas as páginas e consulta cada nome de pregão, mantendo as linhas repetidas")]
    public async Task Get_ReadsEveryPageOfEveryName_KeepingRepeatedRows()
    {
        // Arrange
        _client.GetCashDividendsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var payload = Decode(call.ArgAt<string>(0));
                var name = payload.GetProperty("tradingName").GetString();
                var page = payload.GetProperty("pageNumber").GetInt32();

                return name == "SADIA S/A"
                    ? Page(1, 1, Cash("0,10", "10/01/2005"))
                    : page == 1
                        ? Page(1, 2, Cash("0,55", "25/04/2024"))
                        : Page(2, 2, Cash("0,55", "25/04/2024"));
            });

        // Act
        var fetch = await _provider.GetAsync("SDIA", ["SADIA S.A.", "SADIA S/A"]);

        // Assert
        Assert.Equal(3, fetch.Items.Count);
        Assert.Equal(2, fetch.Items.Count(item => item.Amount == 0.55m));
        await _client.Received(3).GetCashDividendsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Pede ao endpoint de proventos o nome em JSON codificado em base64, página a página")]
    public async Task Get_SendsTheBase64Payload()
    {
        // Arrange
        string? payload = null;
        _client.GetCashDividendsAsync(Arg.Do<string>(sent => payload = sent), Arg.Any<CancellationToken>())
            .Returns(new B3CashDividendsResponse());

        // Act
        await _provider.GetAsync("PETR", ["PETROBRAS"]);

        // Assert
        Assert.Equal(
            """{"language":"pt-br","pageNumber":1,"pageSize":100,"tradingName":"PETROBRAS"}""",
            Encoding.UTF8.GetString(Convert.FromBase64String(payload!)));
    }

    [Fact(DisplayName = "Pula e conta o provento de tipo desconhecido, valor negativo ou sem data-com")]
    public async Task Get_SkipsAndCountsUnusableCashRecords()
    {
        // Arrange
        CashReturns(
            """
            { "page": { "totalPages": 1 },
              "results": [
                { "typeStock": "ON", "valueCash": "0,10", "ratio": "1", "corporateAction": "SUBSCRICAO", "lastDatePriorEx": "21/08/2026" },
                { "typeStock": "ON", "valueCash": "-0,10", "ratio": "1", "corporateAction": "DIVIDENDO", "lastDatePriorEx": "21/08/2026" },
                { "typeStock": "ON", "valueCash": "0,10", "ratio": "1", "corporateAction": "DIVIDENDO", "lastDatePriorEx": "01/01/1900" },
                { "typeStock": "ON", "valueCash": "0,10", "ratio": "0", "corporateAction": "DIVIDENDO", "lastDatePriorEx": "21/08/2026" },
                { "typeStock": "", "valueCash": "0,10", "ratio": "1", "corporateAction": "DIVIDENDO", "lastDatePriorEx": "21/08/2026" },
                { "typeStock": "ON", "valueCash": "0,10", "ratio": "1", "corporateAction": "DIVIDENDO", "lastDatePriorEx": "21/08/2026" }
              ] }
            """);

        // Act
        var fetch = await _provider.GetAsync("PETR", ["PETROBRAS"]);

        // Assert
        Assert.Equal(5, fetch.Skipped);
        Assert.Single(fetch.Items);
    }

    [Fact(DisplayName = "Arredonda o valor para dez casas, a precisão gravada, para a reimportação bater")]
    public async Task Get_RoundsToTheStoredPrecision()
    {
        // Arrange
        CashReturns(
            """
            { "page": { "totalPages": 1 },
              "results": [ { "typeStock": "ON", "valueCash": "0,123456789012345", "ratio": "1",
                             "corporateAction": "DIVIDENDO", "lastDatePriorEx": "21/08/2026" } ] }
            """);

        // Act
        var fetch = await _provider.GetAsync("PETR", ["PETROBRAS"]);

        // Assert
        Assert.Equal(0.1234567890m, Assert.Single(fetch.Items).Amount);
    }

    [Fact(DisplayName = "Mapeia desdobramento, grupamento e bonificação com ISIN, fator e classe derivada do ISIN")]
    public async Task Get_MapsTheStockEvents()
    {
        // Arrange
        SupplementReturns(
            """
            [ { "code": "PETR", "stockDividends": [
                { "isinCode": "BRPETRACNOR9", "label": "DESDOBRAMENTO", "factor": "100,00000000000", "approvedOn": "25/04/2008", "lastDatePrior": "25/04/2008" },
                { "isinCode": "BRPETRACNPR6", "label": "GRUPAMENTO", "factor": "0,01000000000", "approvedOn": "21/06/2000", "lastDatePrior": "21/06/2000" },
                { "isinCode": "BRELETACNPB7", "label": "BONIFICACAO", "factor": "33,33333333300", "approvedOn": "25/03/1994", "lastDatePrior": "25/03/1994" },
                { "isinCode": "BRCPNEACNPA7", "label": "BONIFICAÇÃO", "factor": "10,00000000000", "approvedOn": "01/01/1900", "lastDatePrior": "02/03/1995" }
            ] } ]
            """);

        // Act
        var fetch = await _provider.GetAsync("PETR", ["PETROBRAS"]);

        // Assert
        Assert.Equal(0, fetch.Skipped);
        Assert.Equal(4, fetch.Items.Count);

        var split = fetch.Items[0];
        Assert.Equal(CorporateEventKind.StockSplit, split.Kind);
        Assert.Equal("BRPETRACNOR9", split.Isin);
        Assert.Equal("ON", split.ShareClass);
        Assert.Equal(100m, split.Factor);
        Assert.Equal(new DateOnly(2008, 4, 25), split.CumDate);
        Assert.Null(split.Amount);
        Assert.Null(split.PerShares);

        Assert.Equal(CorporateEventKind.ReverseSplit, fetch.Items[1].Kind);
        Assert.Equal("PN", fetch.Items[1].ShareClass);
        Assert.Equal(0.01m, fetch.Items[1].Factor);

        Assert.Equal(CorporateEventKind.BonusShares, fetch.Items[2].Kind);
        Assert.Equal("PNB", fetch.Items[2].ShareClass);
        Assert.Equal(33.333333333m, fetch.Items[2].Factor);

        Assert.Equal(CorporateEventKind.BonusShares, fetch.Items[3].Kind);
        Assert.Equal("PNA", fetch.Items[3].ShareClass);
        Assert.Null(fetch.Items[3].ApprovalDate);
    }

    [Fact(DisplayName = "Pula e conta o evento sobre ações de rótulo desconhecido, fator inválido ou sem ISIN")]
    public async Task Get_SkipsAndCountsUnusableStockRecords()
    {
        // Arrange
        SupplementReturns(
            """
            [ { "code": "PETR", "stockDividends": [
                { "isinCode": "BRPETRACNOR9", "label": "CISAO", "factor": "10,0", "lastDatePrior": "25/04/2008" },
                { "isinCode": "BRPETRACNOR9", "label": "DESDOBRAMENTO", "factor": "0,0", "lastDatePrior": "25/04/2008" },
                { "isinCode": "", "label": "DESDOBRAMENTO", "factor": "100,0", "lastDatePrior": "25/04/2008" },
                { "isinCode": "BRPETRACNOR9", "label": "DESDOBRAMENTO", "factor": "100,0", "lastDatePrior": "25/04/2008" }
            ] } ]
            """);

        // Act
        var fetch = await _provider.GetAsync("PETR", ["PETROBRAS"]);

        // Assert
        Assert.Equal(3, fetch.Skipped);
        Assert.Single(fetch.Items);
    }

    [Theory(DisplayName = "Resposta ilegível ou 404 é tratada como vazia, sem derrubar o emissor")]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task Get_TreatsUnreadableAnswersAsEmpty(HttpStatusCode status)
    {
        // Arrange
        var failure = await ApiExceptionFor(status);
        _client.GetCashDividendsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<B3CashDividendsResponse>(_ => throw failure);
        _client.GetSupplementAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<List<B3SupplementCompany>>(_ => throw failure);

        // Act
        var fetch = await _provider.GetAsync("TLPP", ["TELESP"]);

        // Assert
        Assert.Empty(fetch.Items);
        Assert.Equal(0, fetch.Skipped);
    }

    [Fact(DisplayName = "Propaga falha de transporte que não é resposta ilegível ou 404")]
    public async Task Get_PropagatesServerErrors()
    {
        // Arrange
        var failure = await ApiExceptionFor(HttpStatusCode.InternalServerError);
        _client.GetCashDividendsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<B3CashDividendsResponse>(_ => throw failure);

        // Act & Assert
        await Assert.ThrowsAsync<ApiException>(() => _provider.GetAsync("PETR", ["PETROBRAS"]));
    }

    [Fact(DisplayName = "Ignora nomes em branco e repetidos, consultando cada nome uma só vez")]
    public async Task Get_QueriesEachDistinctNameOnce()
    {
        // Arrange
        CashReturns("""{ "page": { "totalPages": 1 }, "results": [] }""");

        // Act
        await _provider.GetAsync("PETR", ["PETROBRAS", "PETROBRAS ", "  ", "PETROBRAS"]);

        // Assert
        await _client.Received(1).GetCashDividendsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private static B3CashDividendsResponse Page(int number, int totalPages, params string[] recordsJson) =>
        JsonSerializer.Deserialize<B3CashDividendsResponse>(
            $$"""{ "page": { "pageNumber": {{number}}, "totalPages": {{totalPages}} }, "results": [{{string.Join(",", recordsJson)}}] }""")!;

    private static string Cash(string value, string cumDate) =>
        $$"""{ "typeStock": "ON", "valueCash": "{{value}}", "ratio": "1", "corporateAction": "DIVIDENDO", "lastDatePriorEx": "{{cumDate}}" }""";

    private static JsonElement Decode(string payload) =>
        JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload))).RootElement.Clone();

    private static Task<ApiException> ApiExceptionFor(HttpStatusCode status) =>
        ApiException.Create(
            "An error occured deserializing the response.",
            new HttpRequestMessage(HttpMethod.Get, "https://sistemaswebb3-listados.b3.com.br/x"),
            HttpMethod.Get,
            new HttpResponseMessage(status),
            new RefitSettings(),
            new JsonException("corpo ilegível"));

    private void CashReturns(string json) =>
        _client.GetCashDividendsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(JsonSerializer.Deserialize<B3CashDividendsResponse>(json)!);

    private void SupplementReturns(string json) =>
        _client.GetSupplementAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(JsonSerializer.Deserialize<List<B3SupplementCompany>>(json)!);
}
