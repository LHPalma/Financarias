using Financarias.Domain.Identity;

namespace Financarias.Application.Identity.Users.DTOs.Results;

public sealed record UserResult(
    Guid Id,
    string Name,
    string Email,
    UserStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);