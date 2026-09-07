using Financarias.Api.Security;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace Financarias.Api.FunctionalTests.Security;

public class HeaderCurrentUserTests
{
    private const string HeaderName = "X-User-Id";

    [Fact(DisplayName = "Devolve o id do usuário quando o header traz um GUID válido")]
    public void UserId_ReturnsId_WhenHeaderCarriesValidGuid()
    {
        // Arrange
        var expected = Guid.CreateVersion7();
        var currentUser = new HeaderCurrentUser(CreateAccessor(expected.ToString()));

        // Act
        var userId = currentUser.UserId;

        // Assert
        Assert.Equal(expected, userId);
    }

    [Fact(DisplayName = "Devolve nulo quando o header não foi enviado")]
    public void UserId_ReturnsNull_WhenHeaderIsAbsent()
    {
        // Arrange
        var currentUser = new HeaderCurrentUser(CreateAccessor(headerValue: null));

        // Act
        var userId = currentUser.UserId;

        // Assert
        Assert.Null(userId);
    }

    [Theory(DisplayName = "Devolve nulo quando o header não é um GUID")]
    [InlineData("   ")]
    [InlineData("1")]
    [InlineData("nao-e-um-guid")]
    [InlineData("00000000-0000-0000-0000-00000000000")]
    public void UserId_ReturnsNull_WhenHeaderIsNotAGuid(string headerValue)
    {
        // Arrange
        var currentUser = new HeaderCurrentUser(CreateAccessor(headerValue));

        // Act
        var userId = currentUser.UserId;

        // Assert
        Assert.Null(userId);
    }

    [Fact(DisplayName = "Devolve nulo fora de uma requisição HTTP")]
    public void UserId_ReturnsNull_WhenThereIsNoHttpContext()
    {
        // Arrange: import, migration e job escrevem sem requisição — a porta precisa tolerar.
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns((HttpContext?)null);

        var currentUser = new HeaderCurrentUser(accessor);

        // Act
        var userId = currentUser.UserId;

        // Assert
        Assert.Null(userId);
    }

    private static IHttpContextAccessor CreateAccessor(string? headerValue)
    {
        var httpContext = new DefaultHttpContext();

        if (headerValue is not null)
        {
            httpContext.Request.Headers[HeaderName] = headerValue;
        }

        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);

        return accessor;
    }
}
