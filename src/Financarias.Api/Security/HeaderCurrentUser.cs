using Financarias.Application.Common.Security;

namespace Financarias.Api.Security;

public sealed class HeaderCurrentUser(IHttpContextAccessor contextAccessor)
    : ICurrentUser
{
    private const string HeaderName = "X-User-Id";

    public Guid? UserId
    {
        get
        {
            var header = contextAccessor
                .HttpContext?
                .Request
                .Headers[HeaderName]
                .FirstOrDefault();

            return Guid.TryParse(header, out var userId) ? userId : null;
        }
    }
}