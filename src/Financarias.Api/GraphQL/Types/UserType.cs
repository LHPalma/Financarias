using Financarias.Domain.Identity;
using HotChocolate.Types;

namespace Financarias.Api.GraphQL.Types;

/// <summary>
///     Contrato GraphQL do <see cref="User" />: BindFieldsExplicitly expõe só os campos abaixo
///     (CreatedBy/UpdatedBy ficam fora — trilha interna). O VO Email sai achatado como String.
/// </summary>
public sealed class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor.BindFieldsExplicitly();

        descriptor.Description("Usuário do sistema, sem nenhum dado de credencial.");

        descriptor.Field(u => u.Id).Description("Identificador único do usuário.");
        descriptor.Field(u => u.Name).Description("Nome de exibição.");

        descriptor.Field(u => u.Email)
            .Name("email")
            .Description("E-mail de acesso, em minúsculas.")
            .Type<NonNullType<StringType>>()
            .Resolve(context => context.Parent<User>().Email.Value);

        descriptor.Field(u => u.Status).Description("Situação da conta.");
        descriptor.Field(u => u.CreatedAt).Description("Instante da criação.");
        descriptor.Field(u => u.UpdatedAt)
            .Description("Instante da última alteração, inclusive troca de senha e mudança de situação.");
    }
}
