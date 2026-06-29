namespace Financarias.Application.Analytics.DTOs.Requests;

public sealed record CalculateNtnbPriceRequest(
    decimal VnaBase,
    decimal Yield,
    decimal Inflation,
    DateOnly TradeDate,
    DateOnly DueDate);
