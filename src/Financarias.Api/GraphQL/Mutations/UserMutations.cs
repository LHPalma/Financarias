using Financarias.Application.Identity.Users.DTOs.Requests;
using Financarias.Application.Identity.Users.DTOs.Results;
using Financarias.Application.Identity.Users.UseCases;
using HotChocolate.Authorization;

namespace Financarias.Api.GraphQL.Mutations;

[ExtendObjectType(OperationTypeNames.Mutation)]
public class UserMutations
{
    [GraphQLName("createUser")]
    public Task<UserResult> CreateUserAsync(
        CreateUserRequest input,
        ICreateUserUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(input, cancellationToken);

    [GraphQLName("login")]
    public Task<AccessTokenResult> LoginAsync(
        LoginRequest input,
        ILoginUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(input, cancellationToken);

    [GraphQLName("changePassword")]
    [Authorize]
    public Task<UserResult> ChangePasswordAsync(
        ChangePasswordRequest input,
        IChangePasswordUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(input, cancellationToken);

    [GraphQLName("activateUser")]
    [Authorize]
    public Task<UserResult> ActivateUserAsync(
        Guid id,
        IActivateUserUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(id, cancellationToken);

    [GraphQLName("deactivateUser")]
    [Authorize]
    public Task<UserResult> DeactivateUserAsync(
        Guid id,
        IDeactivateUserUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(id, cancellationToken);
}
