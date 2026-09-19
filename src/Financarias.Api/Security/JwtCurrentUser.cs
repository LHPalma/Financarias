using System.Security.Claims;
using Financarias.Application.Common.Security;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Financarias.Api.Security;

public sealed class JwtCurrentUser(IHttpContextAccessor contextAccessor)
    : ICurrentUser
{
    public Guid? UserId
    {
        get
        {
            var principal = contextAccessor.HttpContext?.User;

            if (principal?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            return Guid.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId)
                ? userId
                : null;
        }
    }
}