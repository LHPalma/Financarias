namespace Financarias.Application.MarketData.Fuel.DTOs.Results;

/// <summary>Comparação, em um posto, entre o preço do etanol e o da gasolina comum.</summary>
/// <param name="StationName">Nome do posto.</param>
/// <param name="Brand">Bandeira do posto.</param>
/// <param name="Municipality">Município do posto.</param>
/// <param name="State">Sigla da UF do posto.</param>
/// <param name="EthanolPrice">Preço mais recente do etanol no posto, em reais por litro.</param>
/// <param name="GasolinePrice">Preço mais recente da gasolina comum no posto, em reais por litro.</param>
/// <param name="Ratio">Preço do etanol dividido pelo da gasolina (0,68 significa que o etanol custa 68% da gasolina).</param>
/// <param name="IsEthanolAdvantageous">Verdadeiro quando a razão é menor que 0,7: pela regra dos 70%, o etanol compensa.</param>
public sealed record EthanolGasolineParityResult(
    string StationName,
    string Brand,
    string Municipality,
    string State,
    decimal EthanolPrice,
    decimal GasolinePrice,
    decimal Ratio,
    bool IsEthanolAdvantageous);