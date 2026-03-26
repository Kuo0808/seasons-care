using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SeasonsCare.Api.Tests.Shared;

namespace SeasonsCare.Api.Tests.Shared.Http;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var requestedUserId = Request.Headers.TryGetValue("X-Test-UserId", out var headerUserId)
            && Guid.TryParse(headerUserId.ToString(), out var parsedUserId)
                ? parsedUserId
                : TestUsers.DefaultUserId;

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, requestedUserId.ToString()),
            new Claim(ClaimTypes.Name, "integration-test-user")
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
