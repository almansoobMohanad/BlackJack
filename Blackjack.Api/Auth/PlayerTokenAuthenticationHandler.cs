using System.Security.Claims;
using System.Text.Encodings.Web;
using Blackjack.Api.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Blackjack.Api.Auth;

// Works out which player is calling from the "Authorization: Bearer <token>" header.
// Controllers marked [Authorize] then only run for a known player.
public class PlayerTokenAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    BlackjackDbContext db)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "PlayerToken";

    private const string BearerPrefix = "Bearer ";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? header = Request.Headers.Authorization;
        if (header is null || !header.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        string tokenHash = PlayerTokens.Hash(header[BearerPrefix.Length..].Trim());
        var player = await db.Players
            .AsNoTracking()
            .Where(p => p.TokenHash == tokenHash)
            .Select(p => new { p.Id, p.Name })
            .SingleOrDefaultAsync();

        if (player is null)
        {
            return AuthenticateResult.Fail("Unknown player token.");
        }

        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, player.Id.ToString()),
                new Claim(ClaimTypes.Name, player.Name)
            ],
            SchemeName);

        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
    }
}
