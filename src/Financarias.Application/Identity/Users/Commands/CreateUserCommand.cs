using Financarias.Application.Common.Messaging;
using Financarias.Domain.Contacts;
using Financarias.Domain.Identity;

namespace Financarias.Application.Identity.Users.Commands;

public sealed record CreateUserCommand(
    string Name,
    Email Email
) : ICommand<User>;