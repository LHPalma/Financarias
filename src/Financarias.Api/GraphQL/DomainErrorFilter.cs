using Financarias.Domain.Common.Exceptions;
using HotChocolate;
using HotChocolate.Execution;

namespace Financarias.Api.GraphQL;

public sealed class DomainErrorFilter : IErrorFilter
{
    public IError OnError(IError error)
    {
        if (error.Exception is BaseDomainException domain)
        {
            return ErrorBuilder.FromError(error)
                .SetMessage(domain.Message)
                .SetCode(domain.Code)
                .SetException(null)
                .Build();
        }

        return error;
    }
}
