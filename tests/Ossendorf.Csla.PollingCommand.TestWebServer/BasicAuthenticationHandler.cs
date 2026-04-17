using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace Ossendorf.Csla.PollingCommand.TestWebServer;

/// <inheritdoc />
public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions> {
    public const string AuthenticationScheme = "Basic";

    public BasicAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder) {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync() {
        if (!Request.Headers.ContainsKey("Authorization")) {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        try {
            var authHeader = AuthenticationHeaderValue.Parse(Request.Headers["Authorization"]);
            var credentialBytes = Convert.FromBase64String(authHeader.Parameter ?? string.Empty);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':', 2);
            var username = credentials[0];
            var password = credentials[1];

            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, username), new Claim(ClaimTypes.Name, username) };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        } catch {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));
        }
    }
}