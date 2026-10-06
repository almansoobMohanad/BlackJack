using Blackjack.Api.Data;
using Blackjack.Contracts;
using Blackjack.Core;
using Microsoft.EntityFrameworkCore;

namespace Blackjack.Api.Services;

// Runs blackjack rounds on the server. The browser only ever says "deal", "hit" or "stand";
// the deck, the rules and the payouts all live here.
public class GameService(BlackjackDbContext db, WalletService wallets, IDeckFactory decks, TimeProvider clock)
{
    public async Task<RoundDto?> GetCurrentRoundAsync(int playerId)
    {
        var entity = await db.GameRounds
            .AsNoTracking()
            .SingleOrDefaultAsync(r => r.PlayerId == playerId && r.State != RoundState.RoundOver);

        if (entity is null)
        {
            return null;
        }

        int balance = await db.Wallets.Where(w => w.PlayerId == playerId).Select(w => w.Balance).SingleAsync();
        return RoundMapper.ToDto(entity, RoundMapper.Restore(entity), balance);
    }

    public Task<RoundDto> DealAsync(int playerId, int bet) => db.InTransactionAsync(async () =>
    {
        var wallet = await wallets.LockWalletAsync(playerId);

        if (await db.GameRounds.AnyAsync(r => r.PlayerId == playerId && r.State != RoundState.RoundOver))
        {
            throw ApiException.Conflict("Finish your current round first.");
        }
        if (bet < GameLimits.MinBet || bet > GameLimits.MaxBet)
        {
            throw ApiException.BadRequest($"Bets must be between {GameLimits.MinBet} and {GameLimits.MaxBet}.");
        }

        var round = BlackjackRound.Deal(decks.Create(), bet);

        var entity = new GameRound
        {
            PlayerId = playerId,
            Bet = bet,
            StartedAt = clock.GetUtcNow().UtcDateTime
        };
        db.GameRounds.Add(entity);
        wallets.Record(wallet, TransactionType.Bet, -bet, entity);

        await SaveAsync(entity, round, wallet);
        return RoundMapper.ToDto(entity, round, wallet.Balance);
    });

    public Task<RoundDto> HitAsync(int playerId, long roundId) =>
        PlayAsync(playerId, roundId, round => round.Hit());

    public Task<RoundDto> StandAsync(int playerId, long roundId) =>
        PlayAsync(playerId, roundId, round => round.Stand());

    private Task<RoundDto> PlayAsync(int playerId, long roundId, Action<BlackjackRound> move) => db.InTransactionAsync(async () =>
    {
        var wallet = await wallets.LockWalletAsync(playerId);

        // Filtering on PlayerId means nobody can act on someone else's round.
        var entity = await db.GameRounds.SingleOrDefaultAsync(r => r.Id == roundId && r.PlayerId == playerId)
            ?? throw ApiException.NotFound("Round not found.");

        if (entity.State != RoundState.PlayerTurn)
        {
            throw ApiException.Conflict("This round is already over.");
        }

        var round = RoundMapper.Restore(entity);
        move(round);

        // If the player stood (or hit to 21), the dealer plays out the rest straight away.
        round.PlayDealerTurn();

        await SaveAsync(entity, round, wallet);
        return RoundMapper.ToDto(entity, round, wallet.Balance);
    });

    // Writes the round back to its row. If it just finished, also pays out and updates
    // the leaderboard, all in the caller's transaction.
    private async Task SaveAsync(GameRound entity, BlackjackRound round, Wallet wallet)
    {
        RoundMapper.CopyCards(round, entity);

        if (round.State == RoundState.RoundOver)
        {
            var now = clock.GetUtcNow().UtcDateTime;
            entity.Result = round.Result;
            entity.Payout = round.Payout;
            entity.FinishedAt = now;

            if (round.Payout > 0)
            {
                wallets.Record(wallet, TransactionType.Payout, round.Payout, entity);
            }

            var stats = await db.Leaderboard.SingleAsync(l => l.PlayerId == entity.PlayerId);
            stats.RoundsPlayed++;
            if (round.Winnings > 0)
            {
                stats.RoundsWon++;
            }
            stats.NetWinnings += round.Winnings;
            stats.BiggestWin = Math.Max(stats.BiggestWin, round.Winnings);
            stats.UpdatedAt = now;
        }

        await db.SaveChangesAsync();
    }
}
