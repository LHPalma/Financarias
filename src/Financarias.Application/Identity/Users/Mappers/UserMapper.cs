using Financarias.Application.Identity.Users.DTOs.Results;
using Financarias.Domain.Contacts;
using Financarias.Domain.Identity;
using Riok.Mapperly.Abstractions;

namespace Financarias.Application.Identity.Users.Mappers;

[Mapper]
public partial class UserMapper
{
    public partial UserResult ToResult(User user);

    private static string ToEmail(Email email) => email.Value;
}