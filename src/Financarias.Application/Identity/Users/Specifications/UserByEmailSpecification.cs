using Ardalis.Specification;
using Financarias.Domain.Contacts;
using Financarias.Domain.Identity;

namespace Financarias.Application.Identity.Users.Specifications;

public sealed class UserByEmailSpecification
    : Specification<User>
{
    public UserByEmailSpecification(Email email) =>
        Query.Where(user => user.Email == email);
}