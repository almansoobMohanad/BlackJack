using Blackjack.Api.Data;
using Blackjack.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Blackjack.Api.Services;

// The only code that changes a wallet balance. Every change also adds a row to the
// Transactions table, so any balance can be traced back through the audit trail.
public class WalletService(BlackjackDbContext db, TimeProvider clock)
{
    private const int HistoryLength = 10;

    // Reads the player's wallet and locks its row until the current transaction ends.
    // Requests for the same player then queue up one after another instead of racing.
    public Task<Wallet> LockWalletAsync(int playerId) =>
        db.Wallets
            .FromSql($"SELECT * FROM Wallets WITH (UPDLOCK, ROWLOCK) WHERE PlayerId = {playerId}")
            .SingleAsync();

    // Changes the balance and records why. Must be called inside a database transaction.
    public void Record(Wallet wallet, TransactionType type, int amount, GameRound? round = null)
    {
        if (db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("Wallet changes must happen inside a database transaction.");
        }

        int newBalance = wallet.Balance + amount;
        if (newBalance < 0)
        {
            throw ApiException.BadRequest("You don't have enough chips for that.");
        }

        var now = clock.GetUtcNow().UtcDateTime;
        wallet.Balance = newBalance;
        wallet.UpdatedAt = now;

        db.Transactions.Add(new WalletTransaction
        {
            Wallet = wallet,
            Type = type,
            Amount = amount,
            BalanceAfter = newBalance,
            GameRound = round,
            CreatedAt = now
        });
    }

    public async Task<WalletDto> GetAsync(int playerId)
    {
        var wallet = await db.Wallets.AsNoTracking().SingleAsync(w => w.PlayerId == playerId);

        var history = await db.Transactions
            .AsNoTracking()
            .Where(t => t.WalletId == wallet.Id)
            .OrderByDescending(t => t.Id)
            .Take(HistoryLength)
            .Select(t => new TransactionDto(t.Id, t.Type, t.Amount, t.BalanceAfter, t.GameRoundId, t.CreatedAt))
            .ToListAsync();

        return new WalletDto(wallet.Balance, history);
    }

    // Tops a broke player back up to the starting balance so they can keep playing.
    public async Task<WalletDto> RefillAsync(int playerId)
    {
        await db.InTransactionAsync(async () =>
        {
            var wallet = await LockWalletAsync(playerId);

            if (wallet.Balance >= GameLimits.MinBet)
            {
                throw ApiException.BadRequest("You can only refill when you're out of chips.");
            }

            Record(wallet, TransactionType.Refill, GameLimits.StartingBalance - wallet.Balance);
            return await db.SaveChangesAsync();
        });

        return await GetAsync(playerId);
    }
}
