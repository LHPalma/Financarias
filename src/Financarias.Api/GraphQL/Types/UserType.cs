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

        descriptor.Field(u => u.Id);
        descriptor.Field(u => u.Name);

        descriptor.Field(u => u.Email)
            .Name("email")
            .Type<NonNullType<StringType>>()
            .Resolve(context => context.Parent<User>().Email.Value);

        descriptor.Field(u => u.Status);
        descriptor.Field(u => u.CreatedAt);
        descriptor.Field(u => u.UpdatedAt);
    }
}
