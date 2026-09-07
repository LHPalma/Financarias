using Financarias.Domain.Common;
using Financarias.Domain.Contacts;

namespace Financarias.Domain.Identity;

public class User :
    BaseEntity<Guid>,
    IAggregateRoot,
    IAuditable
{
    private User()
    {
    }

    private User(string name, Email email)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw IdentityErrors.UserName();
        }

        Id = Guid.CreateVersion7();
        Name = name.Trim();
        Email = email;
        Status = UserStatus.Active;
    }

    public string Name { get; private set; } = null!;

    public Email Email { get; private set; } = null!;

    public UserStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    public static User Create(string name, Email email) => new(name, email);

    public void Deactivate() => Status = UserStatus.Inactive;

    public void Activate() => Status = UserStatus.Active;
}