using Financarias.Application.Addresses.Queries;
using Financarias.Application.Addresses.UseCases;
using Financarias.Application.Addresses;
using Financarias.Application.Common.Messaging;
using Financarias.Domain.Addresses;
using Financarias.Domain.Common.Exceptions;
using NSubstitute;

namespace Financarias.Application.UnitTests.Addresses.UseCases;

public class FindAddressByCepUseCaseTests
{
    private readonly IQueryHandler<FindAddressByCepQuery, AddressLookupResult?> _handler;
    private readonly FindAddressByCepUseCase _useCase;

    public FindAddressByCepUseCaseTests()
    {
        _handler = Substitute.For<IQueryHandler<FindAddressByCepQuery, AddressLookupResult?>>();
        _useCase = new FindAddressByCepUseCase(_handler);
    }

    [Fact(DisplayName = "Normaliza o CEP, delega ao handler e retorna o endereço")]
    public async Task Execute_WithValidCep_DelegatesToHandlerAndReturnsAddress()
    {
        // Arrange
        var expected = new AddressLookupResult("Praça da Sé", "Sé", "São Paulo", "SP", "lado ímpar");
        _handler
            .HandleAsync(new FindAddressByCepQuery(Cep.Create("01001000")), Arg.Any<CancellationToken>())
            .Returns(expected);

        // Act
        var result = await _useCase.ExecuteAsync("01001-000");

        // Assert
        Assert.Same(expected, result);
    }

    [Fact(DisplayName = "Lança InvalidCepException e não chama o handler para CEP inválido")]
    public async Task Execute_WithInvalidCep_ThrowsAndDoesNotCallHandler()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<DomainValidationException>(() => _useCase.ExecuteAsync("123"));
        Assert.Equal("address.cep.invalid", exception.Code);
        await _handler.DidNotReceive().HandleAsync(Arg.Any<FindAddressByCepQuery>(), Arg.Any<CancellationToken>());
    }
}
