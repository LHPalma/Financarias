using Financarias.Domain.Common.Exceptions;
using Financarias.Domain.Contacts;
using Financarias.Domain.Identity;

namespace Financarias.Domain.UnitTests.Identity;

public class UserTests
{
    [Fact(DisplayName = "Create nasce ativo, com nome aparado e id gerado")]
    public void Create_SetsProperties_ForValidInput()
    {
        // Arrange
        var email = Email.Create("luiz@example.com");

        // Act
        var user = User.Create("  Luiz Palma  ", email, TestPasswordHashes.Any);

        // Assert
        Assert.Equal("Luiz Palma", user.Name);
        Assert.Equal(email, user.Email);
        Assert.Equal(UserStatus.Active, user.Status);
        Assert.Equal(TestPasswordHashes.Any, user.PasswordHash);
        Assert.NotEqual(Guid.Empty, user.Id);
    }

    [Fact(DisplayName = "Create gera o id na versão 7, não na 4")]
    public void Create_GeneratesVersion7Id()
    {
        // Act
        var user = CreateUser();

        // Assert
        Assert.Equal(7, user.Id.Version);
    }

    [Theory(DisplayName = "Create lança com o código de nome obrigatório quando o nome está em branco")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Throws_WhenNameIsBlank(string? name)
    {
        // Arrange
        var email = Email.Create("luiz@example.com");

        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() => User.Create(name!, email, TestPasswordHashes.Any));

        Assert.Equal("identity.user.name.required", exception.Code);
    }

    [Fact(DisplayName = "Deactivate torna o usuário inativo")]
    public void Deactivate_SetsStatusToInactive()
    {
        // Arrange
        var user = CreateUser();

        // Act
        user.Deactivate();

        // Assert
        Assert.Equal(UserStatus.Inactive, user.Status);
    }

    [Fact(DisplayName = "Desativar quem já está inativo não tem efeito")]
    public void Deactivate_IsNoOp_WhenAlreadyInactive()
    {
        // Arrange
        var user = CreateUser();
        user.Deactivate();

        // Act
        user.Deactivate();

        // Assert
        Assert.Equal(UserStatus.Inactive, user.Status);
    }

    [Fact(DisplayName = "Activate reativa o usuário")]
    public void Activate_SetsStatusToActive()
    {
        // Arrange
        var user = CreateUser();
        user.Deactivate();

        // Act
        user.Activate();

        // Assert
        Assert.Equal(UserStatus.Active, user.Status);
    }

    [Fact(DisplayName = "Reativar quem já está ativo não tem efeito")]
    public void Activate_IsNoOp_WhenAlreadyActive()
    {
        // Arrange
        var user = CreateUser();

        // Act
        user.Activate();

        // Assert
        Assert.Equal(UserStatus.Active, user.Status);
    }

    [Fact(DisplayName = "ChangePassword troca o hash e não mexe no resto do usuário")]
    public void ChangePassword_ReplacesHash_KeepingTheRest()
    {
        // Arrange
        var user = CreateUser();
        var id = user.Id;
        var email = user.Email;
        var newHash = PasswordHash.Create(
            "$argon2id$v=19$m=19456,t=2,p=1$b3V0cm9zYWx0$b3V0cm9oYXNo",
            pepperVersion: 2);

        // Act
        user.ChangePassword(newHash);

        // Assert
        Assert.Equal(newHash, user.PasswordHash);
        Assert.NotEqual(TestPasswordHashes.Any, user.PasswordHash);
        Assert.Equal(id, user.Id);
        Assert.Equal("Luiz Palma", user.Name);
        Assert.Equal(email, user.Email);
        Assert.Equal(UserStatus.Active, user.Status);
    }

    [Fact(DisplayName = "O agregado não carimba auditoria — isso é trabalho da infraestrutura")]
    public void Create_LeavesAuditFieldsUntouched()
    {
        // Act
        var user = CreateUser();

        // Assert
        Assert.Equal(default(DateTimeOffset), user.CreatedAt);
        Assert.Equal(default(DateTimeOffset), user.UpdatedAt);
        Assert.Null(user.CreatedBy);
        Assert.Null(user.UpdatedBy);
    }

    private static User CreateUser() => User.Create("Luiz Palma", Email.Create("luiz@example.com"), TestPasswordHashes.Any);
}
