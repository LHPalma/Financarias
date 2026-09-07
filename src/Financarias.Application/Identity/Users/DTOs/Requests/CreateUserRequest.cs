namespace Financarias.Application.Identity.Users.DTOs.Requests;

public sealed record CreateUserRequest(
    string Name,
    string Email);
