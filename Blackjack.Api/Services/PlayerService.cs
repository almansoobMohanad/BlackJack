using System.Text.RegularExpressions;
using Blackjack.Api.Auth;
using Blackjack.Api.Data;
using Blackjack.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Blackjack.Api.Services;

public partial class PlayerService(BlackjackDbContext db, WalletService wallets, GameService games, TimeProvider clock)
{
    [GeneratedRegex(@"^[A-Za-z0-9 _\-]+$")]
    private static partial Regex AllowedName();

    // Creates the player, their wallet (with sign-up chips) and their leaderboard row together.
    public async Task<PlayerSessionDto> CreateAsync(string? name)
    {
        name = name?.Trim() ?? "";
        if (name.Length == 0 || name.Length > GameLimits.MaxNameLength || !AllowedName().IsMatch(name))
        {
            throw ApiException.BadRequest(
                $"Names must be 1-{GameLimits.MaxNameLength} characters: letters, numbers, spaces, - or _.");
        }
        if (await db.Players.AnyAsync(p => p.Name == name))
        {
            throw ApiException.Conflict("That name is already taken.");
        }

        string token = PlayerTokens.Create();
        var now = clock.GetUtcNow().UtcDateTime;

        return await db.InTransactionAsync(async () =>
        {
            var player = new Player
            {
                Name = name,
                TokenHash = PlayerTokens.Hash(token),
                CreatedAt = now,
                Wallet = new Wallet { UpdatedAt = now },
                LeaderboardEntry = new LeaderboardEntry { UpdatedAt = now }
            };
            db.Players.Add(player);
            wallets.Record(player.Wallet, TransactionType.SignUpBonus, GameLimits.StartingBalance);

            await db.SaveChangesAsync();
            return new PlayerSessionDto(player.Id, player.Name, token, player.Wallet.Balance);
        });
    }

    public async Task<ProfileDto> GetProfileAsync(int playerId)
    {
        var player = await db.Players
            .AsNoTracking()
            .Where(p => p.Id == playerId)
            .Select(p => new { p.Name, p.Wallet.Balance })
            .SingleAsync();

        return new ProfileDto(playerId, player.Name, player.Balance, await games.GetCurrentRoundAsync(playerId));
    }
}
