using System.Text.RegularExpressions;
using Financarias.Domain.Common;

namespace Financarias.Domain.MarketData.CorporateEvents;

public sealed partial class CorporateEvent : BaseEntity<long>, IAggregateRoot
{
    [GeneratedRegex("^[A-Z0-9]{4}$")]
    private static partial Regex IssuerCodePattern();

    private CorporateEvent()
    {
    }

    private CorporateEvent(
        string issuerCode,
        string? isin,
        string shareClass,
        CorporateEventKind kind,
        DateOnly cumDate,
        DateOnly? approvalDate,
        DateOnly? paymentDate,
        decimal? amount,
        int? perShares,
        decimal? factor,
        string label)
    {
        EnsureValid(issuerCode, shareClass, kind, amount, perShares, factor);

        IssuerCode = issuerCode;
        Isin = isin;
        ShareClass = shareClass;
        Kind = kind;
        CumDate = cumDate;
        ApprovalDate = approvalDate;
        PaymentDate = paymentDate;
        Amount = amount;
        PerShares = perShares;
        Factor = factor;
        Label = label;
    }

    public string IssuerCode { get; private set; } = null!;

    public string? Isin { get; private set; }

    public string ShareClass { get; private set; } = null!;

    public CorporateEventKind Kind { get; private set; }

    public DateOnly CumDate { get; private set; }

    public DateOnly? ApprovalDate { get; private set; }

    public DateOnly? PaymentDate { get; private set; }

    public decimal? Amount { get; private set; }

    public int? PerShares { get; private set; }

    public decimal? Factor { get; private set; }

    public string Label { get; private set; } = null!;

    public decimal? AmountPerShare => Amount / PerShares;

    public static bool IsValidIssuerCode(string? issuerCode) =>
        issuerCode is not null && IssuerCodePattern().IsMatch(issuerCode);

    public static bool IsCash(CorporateEventKind kind) =>
        kind is CorporateEventKind.CashDividend or CorporateEventKind.InterestOnEquity or CorporateEventKind.Yield;

    public static CorporateEvent Create(
        string issuerCode,
        string? isin,
        string shareClass,
        CorporateEventKind kind,
        DateOnly cumDate,
        DateOnly? approvalDate,
        DateOnly? paymentDate,
        decimal? amount,
        int? perShares,
        decimal? factor,
        string label) =>
        new(issuerCode, isin, shareClass, kind, cumDate, approvalDate, paymentDate, amount, perShares, factor, label);

    public static string? GetError(
        string issuerCode,
        string shareClass,
        CorporateEventKind kind,
        decimal? amount,
        int? perShares,
        decimal? factor)
    {
        if (!IsValidIssuerCode(issuerCode))
        {
            return $"issuer code must be 4 letters or digits, got '{issuerCode}'";
        }

        if (string.IsNullOrWhiteSpace(shareClass))
        {
            return "share class is required";
        }

        if (IsCash(kind))
        {
            if (amount is null || amount < 0)
            {
                return $"a {kind} needs a non-negative amount, got {amount}";
            }

            if (perShares is null or <= 0)
            {
                return $"a {kind} needs a positive number of shares per quote, got {perShares}";
            }

            return factor is not null ? $"a {kind} cannot have a factor" : null;
        }

        if (factor is null or <= 0)
        {
            return $"a {kind} needs a positive factor, got {factor}";
        }

        return amount is not null || perShares is not null ? $"a {kind} cannot have an amount" : null;
    }

    private static void EnsureValid(
        string issuerCode,
        string shareClass,
        CorporateEventKind kind,
        decimal? amount,
        int? perShares,
        decimal? factor)
    {
        if (GetError(issuerCode, shareClass, kind, amount, perShares, factor) is { } reason)
        {
            throw MarketDataErrors.CorporateEvent(reason);
        }
    }
}
