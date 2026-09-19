namespace Financarias.Application.Identity.Users.DTOs.Requests;

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);