using Financarias.Application.Common.Persistence;
using Financarias.Application.Holidays.Import;
using Financarias.Application.Holidays.UseCases;
using Financarias.Domain.Holidays.Models;
using NSubstitute;

namespace Financarias.Application.UnitTests.Holidays.UseCases;

public class ImportHolidaysUseCaseTests
{
    [Fact(DisplayName = "Não consulta nem persiste quando o provedor não retorna feriados")]
    public async Task Execute_WhenProviderReturnsEmpty_PersistsNothing()
    {
        // Arrange
        var provider = Substitute.For<IHolidayProvider>();
        provider.FetchHolidaysAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<HolidayImportItem>)[]);
        var repository = Substitute.For<IRepository<Holiday>>();
        var useCase = new ImportHolidaysUseCase(provider, Substitute.For<IApplicationDbContext>(), repository);

        // Act
        var result = await useCase.ExecuteAsync();

        // Assert
        Assert.Equal(0, result.TotalFetched);
        Assert.Equal(0, result.TotalSaved);
        await repository.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<Holiday>>(), Arg.Any<CancellationToken>());
    }
}
