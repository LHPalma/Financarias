namespace Financarias.Application.Addresses;

public sealed record AddressLookupResult(
    string? Street,
    string? Neighborhood,
    string? City,
    string? State,
    string? Complement
);