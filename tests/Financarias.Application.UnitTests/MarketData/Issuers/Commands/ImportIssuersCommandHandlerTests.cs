using Ardalis.Specification;
using Financarias.Application.Common.Persistence;
using Financarias.Application.MarketData.Issuers.Commands;
using Financarias.Application.MarketData.Issuers.Import;
using Financarias.Domain.LegalEntities;
using Financarias.Domain.MarketData.Issuers;
using NSubstitute;

namespace Financarias.Application.UnitTests.MarketData.Issuers.Commands;

public class ImportIssuersCommandHandlerTests
{
    private readonly IIssuerProvider _provider = Substitute.For<IIssuerProvider>();
    private readonly IRepository<Issuer> _repository = Substitute.For<IRepository<Issuer>>();

    [Fact(DisplayName = "Cria os emissores novos e conta os que a B3 não lista")]
    public async Task Handle_CreatesNewIssuers()
    {
        // Arrange
        GivenFetch([Item("PETR", sector: "Petróleo"), Item("VALE", sector: "Mineração")], notListed: 1);
        GivenExisting();
        List<Issuer>? added = null;
        await _repository.AddRangeAsync(Arg.Do<IEnumerable<Issuer>>(items => added = items.ToList()), Arg.Any<CancellationToken>());

        // Act
        var result = await CreateHandler().HandleAsync(new ImportIssuersCommand(["PETR", "VALE", "ARCZ"]));

        // Assert
        Assert.Equal(new IssuerImportResult(3, 1, 2, 0), result);
        Assert.Equal(["PETR", "VALE"], added!.Select(issuer => issuer.IssuerCode));
        Assert.Equal("Mineração", added.Single(issuer => issuer.IssuerCode == "VALE").Sector);
        Assert.Equal("33000167000101", added.Single(issuer => issuer.IssuerCode == "PETR").Cnpj?.Value);
    }

    [Fact(DisplayName = "CNPJ que a B3 devolve inválido vira cadastro sem CNPJ, não erro")]
    public async Task Handle_InvalidCnpj_BecomesNull()
    {
        // Arrange
        GivenFetch([Item("PETR", cnpj: "123")], notListed: 0);
        GivenExisting();
        List<Issuer>? added = null;
        await _repository.AddRangeAsync(Arg.Do<IEnumerable<Issuer>>(items => added = items.ToList()), Arg.Any<CancellationToken>());

        // Act
        await CreateHandler().HandleAsync(new ImportIssuersCommand(["PETR"]));

        // Assert
        Assert.Null(Assert.Single(added!).Cnpj);
    }

    [Fact(DisplayName = "Atualiza o emissor que mudou na B3 e salva sem criar nada")]
    public async Task Handle_UpdatesAChangedIssuer()
    {
        // Arrange
        var existing = Issuer.Create("PETR", "9512", "PETROBRAS SA", "PETROBRAS", Cnpj.Create("33000167000101"), "Antigo", null, null, null, null, null);
        GivenFetch([Item("PETR", sector: "Petróleo")], notListed: 0);
        GivenExisting(existing);

        // Act
        var result = await CreateHandler().HandleAsync(new ImportIssuersCommand(["PETR"]));

        // Assert
        Assert.Equal(new IssuerImportResult(1, 0, 0, 1), result);
        Assert.Equal("Petróleo", existing.Sector);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<Issuer>>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Não grava nada quando os cadastros já estão iguais aos da B3")]
    public async Task Handle_ChangesNothing_WhenTheDataIsTheSame()
    {
        // Arrange
        var existing = Issuer.Create("PETR", "9512", "PETROLEO BRASILEIRO S.A. PETROBRAS", "PETROBRAS", Cnpj.Create("33000167000101"), null, null, null, null, null, null);
        GivenFetch([Item("PETR")], notListed: 0);
        GivenExisting(existing);

        // Act
        var result = await CreateHandler().HandleAsync(new ImportIssuersCommand(["PETR"]));

        // Assert
        Assert.Equal(new IssuerImportResult(1, 0, 0, 0), result);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<Issuer>>(), Arg.Any<CancellationToken>());
    }

    private ImportIssuersCommandHandler CreateHandler() => new(_provider, _repository);

    private static IssuerImportItem Item(string code, string? sector = null, string? cnpj = "33000167000101") =>
        new(code, "9512", "PETROLEO BRASILEIRO S.A. PETROBRAS", "PETROBRAS", cnpj, sector, null, null, null, null, null);

    private void GivenFetch(IssuerImportItem[] items, int notListed) =>
        _provider.GetAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new IssuerFetch(items, notListed));

    private void GivenExisting(params Issuer[] issuers) =>
        _repository.ListAsync(Arg.Any<ISpecification<Issuer>>(), Arg.Any<CancellationToken>())
            .Returns(issuers.ToList());
}
