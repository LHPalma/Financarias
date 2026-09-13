namespace Financarias.Application.Identity.Users.DTOs.Requests;

public sealed record LoginRequest(
    string Email,
    string Password);
