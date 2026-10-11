using Financarias.Domain.Common;
using Financarias.Domain.LegalEntities;
using Financarias.Domain.MarketData.CorporateEvents;

namespace Financarias.Domain.MarketData.Issuers;

public sealed class Issuer : BaseEntity<long>, IAggregateRoot
{
    private Issuer()
    {
    }

    private Issuer(
        string issuerCode,
        string cvmCode,
        string companyName,
        string tradingName,
        Cnpj? cnpj,
        string? sector,
        string? subsector,
        string? segment,
        string? listingMarket,
        string? activity,
        string? website)
    {
        EnsureValid(issuerCode, cvmCode, companyName, tradingName);

        IssuerCode = issuerCode;
        CvmCode = cvmCode;
        CompanyName = companyName;
        TradingName = tradingName;
        Cnpj = cnpj;
        Sector = sector;
        Subsector = subsector;
        Segment = segment;
        ListingMarket = listingMarket;
        Activity = activity;
        Website = website;
    }

    public string IssuerCode { get; private set; } = null!;

    public string CvmCode { get; private set; } = null!;

    public string CompanyName { get; private set; } = null!;

    public string TradingName { get; private set; } = null!;

    public Cnpj? Cnpj { get; private set; }

    public string? Sector { get; private set; }

    public string? Subsector { get; private set; }

    public string? Segment { get; private set; }

    public string? ListingMarket { get; private set; }

    public string? Activity { get; private set; }

    public string? Website { get; private set; }

    public static Issuer Create(
        string issuerCode,
        string cvmCode,
        string companyName,
        string tradingName,
        Cnpj? cnpj,
        string? sector,
        string? subsector,
        string? segment,
        string? listingMarket,
        string? activity,
        string? website) =>
        new(issuerCode, cvmCode, companyName, tradingName, cnpj, sector, subsector, segment, listingMarket, activity, website);

    public bool Update(
        string cvmCode,
        string companyName,
        string tradingName,
        Cnpj? cnpj,
        string? sector,
        string? subsector,
        string? segment,
        string? listingMarket,
        string? activity,
        string? website)
    {
        EnsureValid(IssuerCode, cvmCode, companyName, tradingName);

        if (CvmCode == cvmCode
            && CompanyName == companyName
            && TradingName == tradingName
            && Cnpj == cnpj
            && Sector == sector
            && Subsector == subsector
            && Segment == segment
            && ListingMarket == listingMarket
            && Activity == activity
            && Website == website)
        {
            return false;
        }

        CvmCode = cvmCode;
        CompanyName = companyName;
        TradingName = tradingName;
        Cnpj = cnpj;
        Sector = sector;
        Subsector = subsector;
        Segment = segment;
        ListingMarket = listingMarket;
        Activity = activity;
        Website = website;

        return true;
    }

    private static void EnsureValid(string issuerCode, string cvmCode, string companyName, string tradingName)
    {
        if (!CorporateEvent.IsValidIssuerCode(issuerCode))
        {
            throw MarketDataErrors.Issuer($"issuer code must be 4 letters or digits in capitals, got '{issuerCode}'");
        }

        if (string.IsNullOrWhiteSpace(cvmCode))
        {
            throw MarketDataErrors.Issuer("CVM code is required");
        }

        if (string.IsNullOrWhiteSpace(companyName))
        {
            throw MarketDataErrors.Issuer("company name is required");
        }

        if (string.IsNullOrWhiteSpace(tradingName))
        {
            throw MarketDataErrors.Issuer("trading name is required");
        }
    }
}
