using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Blackjack.Api.Auth;

public static class PlayerTokens
{
    // 32 random bytes: far too many possibilities to guess.
    public static string Create() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public static int GetPlayerId(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Request is not signed in."));
}
