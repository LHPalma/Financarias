using Financarias.Application.Common.Messaging;
using Financarias.Domain.Identity;

namespace Financarias.Application.Identity.Users.Commands;

public sealed record DeactivateUserCommand(Guid UserId) : ICommand<User>;
