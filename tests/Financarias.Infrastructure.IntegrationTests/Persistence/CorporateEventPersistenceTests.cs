using Financarias.Domain.MarketData.CorporateEvents;
using Financarias.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Financarias.Infrastructure.IntegrationTests.Persistence;

public class CorporateEventPersistenceTests : IAsyncLifetime
{
    private static readonly DateOnly CumDate = new(2026, 8, 21);

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact(DisplayName = "Persiste e relê um provento em dinheiro com datas, valor e lote")]
    public async Task SaveAndRead_RoundTripsCashEvent()
    {
        // Arrange
        long id;
        await using (var write = CreateContext())
        {
            var cashEvent = Cash(CorporateEventKind.InterestOnEquity, 0.67407131m, approvalDate: new DateOnly(2026, 8, 6), paymentDate: new DateOnly(2026, 11, 23));
            write.CorporateEvents.Add(cashEvent);
            await write.SaveChangesAsync();
            id = cashEvent.Id;
        }

        // Act
        await using var read = CreateContext();
        var loaded = await read.CorporateEvents.SingleAsync(e => e.Id == id);

        // Assert
        Assert.Equal("PETR", loaded.IssuerCode);
        Assert.Equal("BRPETRACNOR9", loaded.Isin);
        Assert.Equal("ON", loaded.ShareClass);
        Assert.Equal(CorporateEventKind.InterestOnEquity, loaded.Kind);
        Assert.Equal(CumDate, loaded.CumDate);
        Assert.Equal(new DateOnly(2026, 8, 6), loaded.ApprovalDate);
        Assert.Equal(new DateOnly(2026, 11, 23), loaded.PaymentDate);
        Assert.Equal(0.67407131m, loaded.Amount);
        Assert.Equal(1, loaded.PerShares);
        Assert.Null(loaded.Factor);
        Assert.Equal(0.67407131m, loaded.AmountPerShare);
    }

    [Fact(DisplayName = "Persiste e relê um desdobramento com o fator de onze casas como publicado")]
    public async Task SaveAndRead_RoundTripsStockEvent()
    {
        // Arrange
        await using (var write = CreateContext())
        {
            write.CorporateEvents.Add(CorporateEvent.Create(
                "PETR", "BRPETRACNPR6", "PN", CorporateEventKind.BonusShares, new DateOnly(1994, 3, 25),
                null, null, null, null, 33.333333333m, "BONIFICACAO"));
            await write.SaveChangesAsync();
        }

        // Act
        await using var read = CreateContext();
        var loaded = await read.CorporateEvents.SingleAsync();

        // Assert
        Assert.Equal(CorporateEventKind.BonusShares, loaded.Kind);
        Assert.Equal(33.333333333m, loaded.Factor);
        Assert.Null(loaded.Amount);
        Assert.Null(loaded.PerShares);
        Assert.Null(loaded.ApprovalDate);
        Assert.Null(loaded.PaymentDate);
    }

    [Fact(DisplayName = "Parcelas idênticas coexistem: não há índice único sobre o evento")]
    public async Task IdenticalInstallments_Coexist()
    {
        // Arrange
        await using var context = CreateContext();
        context.CorporateEvents.Add(Cash(CorporateEventKind.CashDividend, 0.55015787m));
        context.CorporateEvents.Add(Cash(CorporateEventKind.CashDividend, 0.55015787m));

        // Act
        await context.SaveChangesAsync();

        // Assert
        Assert.Equal(2, await context.CorporateEvents.CountAsync());
    }

    [Theory(DisplayName = "CHECK do banco barra campos incompatíveis com o tipo, inseridos fora do agregado")]
    [InlineData("CashDividend", "NULL", "1", "NULL")]              // dinheiro sem valor
    [InlineData("CashDividend", "0.5", "0", "NULL")]               // lote zero
    [InlineData("CashDividend", "0.5", "1", "10")]                 // dinheiro com fator
    [InlineData("StockSplit", "NULL", "NULL", "NULL")]             // ações sem fator
    [InlineData("StockSplit", "NULL", "NULL", "0")]                // fator zero
    [InlineData("BonusShares", "1.0", "NULL", "33.3")]             // ações com valor
    public async Task CheckConstraint_RejectsInvalidRows_InsertedBypassingTheAggregate(
        string kind, string amount, string perShares, string factor)
    {
        // Arrange
        await using var context = CreateContext();

        // Act
        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            context.Database.ExecuteSqlRawAsync(
                $"""
                INSERT INTO corporate_events (issuer_code, share_class, kind, cum_date, amount, per_shares, factor, label)
                VALUES ('PETR', 'ON', '{kind}', '2026-08-21', {amount}, {perShares}, {factor}, 'X')
                """));

        // Assert
        Assert.Equal("ck_corporate_events_fields_by_kind", exception.ConstraintName);
    }

    private static CorporateEvent Cash(
        CorporateEventKind kind,
        decimal amount,
        DateOnly? approvalDate = null,
        DateOnly? paymentDate = null) =>
        CorporateEvent.Create("PETR", "BRPETRACNOR9", "ON", kind, CumDate, approvalDate, paymentDate, amount, 1, null, "X");

    private FinancariasDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FinancariasDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;

        return new FinancariasDbContext(options);
    }
}
