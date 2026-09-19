using Financarias.Application.Common.Messaging;
using Financarias.Domain.Identity;

namespace Financarias.Application.Identity.Users.Commands;

public sealed record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    Password NewPassword
) : ICommand<User>;