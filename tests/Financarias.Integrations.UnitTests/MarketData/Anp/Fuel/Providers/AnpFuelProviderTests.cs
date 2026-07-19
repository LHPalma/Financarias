using System.IO.Compression;
using System.Net;
using System.Text;
using Financarias.Application.MarketData.Fuel.Import;
using Financarias.Domain.Geography;
using Financarias.Domain.MarketData.Fuel;
using Financarias.Integrations.MarketData.Anp.Fuel.Clients;
using Financarias.Integrations.MarketData.Anp.Fuel.Providers;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Refit;

namespace Financarias.Integrations.UnitTests.MarketData.Anp.Fuel.Providers;

public class AnpFuelProviderTests
{
    private const string Header =
        "Regiao - Sigla;Estado - Sigla;Municipio;Revenda;CNPJ da Revenda;Nome da Rua;Numero Rua;Complemento;" +
        "Bairro;Cep;Produto;Data da Coleta;Valor de Venda;Valor de Compra;Unidade de Medida;Bandeira";

    private readonly IAnpFuelClient _client = Substitute.For<IAnpFuelClient>();
    private readonly AnpFuelProvider _provider;

    public AnpFuelProviderTests()
    {
        _provider = new AnpFuelProvider(_client, Substitute.For<ILogger<AnpFuelProvider>>());
    }

    [Fact(DisplayName = "Parseia o CSV da ANP mapeando produto, região e decimal com vírgula")]
    public async Task FetchFuelPrices_MapsRows_FromAnpCsv()
    {
        // Arrange: CNPJ vem com espaço e máscara; valor de compra vazio
        GivenClientReturns(Csv(
            "N;AC;CRUZEIRO DO SUL;POSTO COPACABANA; 01.492.748/0003-83;AVENIDA COPACABANA;440;;COPACABANA;" +
            "69980-000;GASOLINA;02/01/2026;7,97;;R$ / litro;IPIRANGA",
            "SE;SP;SAO PAULO;POSTO PAULISTA;11.222.333/0001-81;AVENIDA PAULISTA;1000;LOJA 2;BELA VISTA;" +
            "01310-100;ETANOL;03/01/2026;4,19;3,50;R$ / litro;VIBRA"));

        // Act
        var items = await CollectAsync();

        // Assert
        Assert.Equal(2, items.Count);

        var gasoline = items[0];
        Assert.Equal("01492748000383", gasoline.Cnpj.Value);
        Assert.Equal("POSTO COPACABANA", gasoline.StationName);
        Assert.Equal("IPIRANGA", gasoline.Brand);
        Assert.Equal(Region.North, gasoline.Region);
        Assert.Equal(FuelProduct.Gasoline, gasoline.Product);
        Assert.Equal(new DateOnly(2026, 1, 2), gasoline.CollectedOn);
        Assert.Equal(7.97m, gasoline.SalePrice);
        Assert.Null(gasoline.PurchasePrice);
        Assert.Equal("69980000", gasoline.PostalCode!.Value);
        Assert.Null(gasoline.Complement);

        var ethanol = items[1];
        Assert.Equal(Region.Southeast, ethanol.Region);
        Assert.Equal(FuelProduct.Ethanol, ethanol.Product);
        Assert.Equal(3.50m, ethanol.PurchasePrice);
        Assert.Equal("LOJA 2", ethanol.Complement);
    }

    [Theory(DisplayName = "Pula linha malformada em vez de derrubar o import (miss não é erro)")]
    [InlineData("N;AC;X;POSTO Z;INVALIDO;R;1;;B;;GASOLINA;02/01/2026;5,00;;R$ / litro;BRANCA")]
    [InlineData("N;AC;X;POSTO Z; 01.492.748/0003-83;R;1;;B;;QUEROSENE;02/01/2026;5,00;;R$ / litro;BRANCA")]
    [InlineData("XX;AC;X;POSTO Z; 01.492.748/0003-83;R;1;;B;;GASOLINA;02/01/2026;5,00;;R$ / litro;BRANCA")]
    [InlineData("N;AC;X;POSTO Z; 01.492.748/0003-83;R;1;;B;;GASOLINA;data-ruim;5,00;;R$ / litro;BRANCA")]
    [InlineData("N;AC;X;POSTO Z; 01.492.748/0003-83;R;1;;B;;GASOLINA;02/01/2026;0,00;;R$ / litro;BRANCA")]
    [InlineData("N;AC;X;; 01.492.748/0003-83;R;1;;B;;GASOLINA;02/01/2026;5,00;;R$ / litro;BRANCA")]
    public async Task FetchFuelPrices_SkipsMalformedRow(string row)
    {
        // Arrange
        GivenClientReturns(Csv(row));

        // Act
        var items = await CollectAsync();

        // Assert
        Assert.Empty(items);
    }

    [Fact(DisplayName = "Cai para o semestre anterior quando o arquivo corrente ainda não existe (404)")]
    public async Task FetchFuelPrices_FallsBackToPreviousSemester_On404()
    {
        // Arrange: primeira chamada 404, segunda devolve o arquivo
        var notFound = await ApiException.Create(
            new HttpRequestMessage(HttpMethod.Get, "https://www.gov.br/ca.zip"),
            HttpMethod.Get,
            new HttpResponseMessage(HttpStatusCode.NotFound),
            new RefitSettings());

        _client.GetFuelPricesFileAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(
                _ => throw notFound,
                _ => Csv("N;AC;CRUZEIRO DO SUL;POSTO COPACABANA; 01.492.748/0003-83;AV;440;;B;69980-000;" +
                         "GASOLINA;02/01/2026;7,97;;R$ / litro;IPIRANGA"));

        // Act
        var items = await CollectAsync();

        // Assert
        Assert.Single(items);
        await _client.Received(2).GetFuelPricesFileAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private async Task<List<FuelPriceImportItem>> CollectAsync()
    {
        var items = new List<FuelPriceImportItem>();
        await foreach (var item in _provider.FetchFuelPricesAsync())
        {
            items.Add(item);
        }

        return items;
    }

    private void GivenClientReturns(Stream zip) =>
        _client.GetFuelPricesFileAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(zip);

    private static Stream Csv(params string[] rows)
    {
        var content = string.Join("\r\n", [Header, .. rows]);

        var zip = new MemoryStream();
        using (var archive = new ZipArchive(zip, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("ca-teste.csv");
            using var entryStream = entry.Open();
            using var writer = new StreamWriter(entryStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            writer.Write(content);
        }

        zip.Position = 0;
        return zip;
    }
}
