using Financarias.Application.Common.Messaging;
using Financarias.Application.Common.Security;
using Financarias.Domain.Contacts;

namespace Financarias.Application.Identity.Users.Commands;

public sealed record LoginCommand(Email Email, string Password)
    : ICommand<IssuedAccessToken>;