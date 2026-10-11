using System.Net;
using Financarias.Integrations.MarketData.B3.Issuers.Clients;
using Financarias.Integrations.MarketData.B3.Issuers.DTOs.Responses;
using Financarias.Integrations.MarketData.B3.Issuers.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Refit;

namespace Financarias.Integrations.UnitTests.MarketData.B3.Issuers.Providers;

public class B3IssuerProviderTests
{
    private readonly IB3IssuerClient _client = Substitute.For<IB3IssuerClient>();
    private readonly B3IssuerProvider _provider;

    public B3IssuerProviderTests()
    {
        _provider = new B3IssuerProvider(_client, NullLogger<B3IssuerProvider>.Instance);
    }

    [Fact(DisplayName = "GetAsync devolve só os emissores pedidos, com a classificação separada em setor, subsetor e segmento")]
    public async Task Get_ReturnsTheRequestedIssuers_WithTheClassificationSplit()
    {
        // Arrange
        GivenListing(
            Company("PETR", "9512", "PETROLEO BRASILEIRO S.A. PETROBRAS", "PETROBRAS   ", "33000167000101"),
            Company("VALE", "4170", "VALE S.A.", "VALE", "33592510000154"),
            Company("UQMU", "900049", "1461 INVESTIMENTOS S.A", "1461 INVEST", "46639922000144"));
        GivenDetail(new B3CompanyDetail
        {
            IndustryClassification = "Petróleo. Gás e Biocombustíveis / Petróleo. Gás e Biocombustíveis / Exploração. Refino e Distribuição",
            Activity = " Pesquisa. lavra. ",
            Website = "www.petrobras.com.br",
            Market = "BOVESPA NIVEL 2",
        });

        // Act
        var fetch = await _provider.GetAsync(["PETR"]);

        // Assert
        var item = Assert.Single(fetch.Items);
        Assert.Equal("PETR", item.IssuerCode);
        Assert.Equal("9512", item.CvmCode);
        Assert.Equal("PETROBRAS", item.TradingName);
        Assert.Equal("33000167000101", item.Cnpj);
        Assert.Equal("Petróleo. Gás e Biocombustíveis", item.Sector);
        Assert.Equal("Petróleo. Gás e Biocombustíveis", item.Subsector);
        Assert.Equal("Exploração. Refino e Distribuição", item.Segment);
        Assert.Equal("BOVESPA NIVEL 2", item.ListingMarket);
        Assert.Equal("Pesquisa. lavra.", item.Activity);
        Assert.Equal(0, fetch.NotListed);
    }

    [Fact(DisplayName = "GetAsync conta os emissores pedidos que a B3 não lista")]
    public async Task Get_CountsTheIssuersThatAreNotListed()
    {
        // Arrange
        GivenListing(Company("PETR", "9512", "PETROBRAS SA", "PETROBRAS", "33000167000101"));
        GivenDetail(new B3CompanyDetail());

        // Act
        var fetch = await _provider.GetAsync(["PETR", "ARCZ", "SDIA"]);

        // Assert
        Assert.Single(fetch.Items);
        Assert.Equal(2, fetch.NotListed);
    }

    [Fact(DisplayName = "GetAsync cadastra o emissor sem classificação quando o detalhe não responde")]
    public async Task Get_WithoutDetail_ReturnsTheIssuerWithoutClassification()
    {
        // Arrange
        GivenListing(Company("PETR", "9512", "PETROBRAS SA", "PETROBRAS", "33000167000101"));
        var exception = await ApiException.Create(
            new HttpRequestMessage(HttpMethod.Get, "http://x"),
            HttpMethod.Get,
            new HttpResponseMessage(HttpStatusCode.NotFound),
            new RefitSettings());
        _client.GetDetailAsync(default!, default).ThrowsAsyncForAnyArgs(exception);

        // Act
        var fetch = await _provider.GetAsync(["PETR"]);

        // Assert
        var item = Assert.Single(fetch.Items);
        Assert.Null(item.Sector);
        Assert.Null(item.ListingMarket);
    }

    [Fact(DisplayName = "GetAsync pula a empresa da lista que vem sem nome ou sem código CVM")]
    public async Task Get_SkipsCompaniesWithoutNameOrCvmCode()
    {
        // Arrange
        GivenListing(Company("PETR", "", "PETROBRAS SA", "PETROBRAS", null), Company("VALE", "4170", " ", "VALE", null));
        GivenDetail(new B3CompanyDetail());

        // Act
        var fetch = await _provider.GetAsync(["PETR", "VALE"]);

        // Assert
        Assert.Empty(fetch.Items);
    }

    [Fact(DisplayName = "GetAsync percorre todas as páginas da lista de empresas")]
    public async Task Get_ReadsEveryPageOfTheListing()
    {
        // Arrange
        var page = 0;
        _client.GetInitialCompaniesAsync(default!, default).ReturnsForAnyArgs(_ =>
        {
            page++;
            return Task.FromResult(new B3InitialCompaniesResponse
            {
                Page = new B3CompaniesPage { TotalPages = 2 },
                Results = [page == 1
                    ? Company("PETR", "9512", "PETROBRAS SA", "PETROBRAS", null)
                    : Company("VALE", "4170", "VALE S.A.", "VALE", null)],
            });
        });
        GivenDetail(new B3CompanyDetail());

        // Act
        var fetch = await _provider.GetAsync(["PETR", "VALE"]);

        // Assert
        Assert.Equal(["PETR", "VALE"], fetch.Items.Select(item => item.IssuerCode));
        Assert.Equal(2, page);
    }

    private static B3ListedCompany Company(string code, string cvm, string name, string tradingName, string? cnpj) =>
        new() { IssuingCompany = code, CodeCvm = cvm, CompanyName = name, TradingName = tradingName, Cnpj = cnpj };

    private void GivenListing(params B3ListedCompany[] companies) =>
        _client.GetInitialCompaniesAsync(default!, default).ReturnsForAnyArgs(new B3InitialCompaniesResponse
        {
            Page = new B3CompaniesPage { TotalPages = 1 },
            Results = [.. companies],
        });

    private void GivenDetail(B3CompanyDetail detail) =>
        _client.GetDetailAsync(default!, default).ReturnsForAnyArgs(detail);
}
