namespace Financarias.Domain.MarketData.Currencies;

/// <summary>
/// Um período em que o Brasil usou uma moeda, com quanto dela vale R$ 1 de hoje e o código com que o arquivo
/// COTAHIST da B3 a chama.
/// </summary>
/// <param name="Name">Nome da moeda. Cruzeiro aparece em dois períodos, com valores diferentes.</param>
/// <param name="From">Primeiro dia em que a moeda vigorou.</param>
/// <param name="To">Último dia em que a moeda vigorou.</param>
/// <param name="DivisorToReais">Quantas unidades da moeda correspondem a R$ 1, pelas conversões legais acumuladas.</param>
/// <param name="ModRef">Código da moeda no campo MODREF do COTAHIST. <c>CR$</c> serve a três períodos.</param>
public sealed record CurrencyEra(string Name, DateOnly From, DateOnly To, decimal DivisorToReais, string ModRef);
