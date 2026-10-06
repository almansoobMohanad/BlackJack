using Blackjack.Contracts;
using Blackjack.Core;

namespace Blackjack.Api.Data;

public class Player
{
    public int Id { get; set; }

    public required string Name { get; set; }

    // SHA-256 hash of the player's secret token. The token itself is never stored,
    // so a leaked database can't be used to log in as anyone.
    public required string TokenHash { get; set; }

    public DateTime CreatedAt { get; set; }

    public Wallet Wallet { get; set; } = null!;

    public LeaderboardEntry LeaderboardEntry { get; set; } = null!;

    public List<GameRound> Rounds { get; set; } = [];
}

public class Wallet
{
    public int Id { get; set; }

    public int PlayerId { get; set; }

    public Player Player { get; set; } = null!;

    // Only WalletService changes this, and always together with a Transactions row.
    public int Balance { get; set; }

    public DateTime UpdatedAt { get; set; }

    // SQL Server bumps this on every update. EF Core checks it when saving, so a stale
    // copy of the wallet can never overwrite a newer balance.
    public byte[] RowVersion { get; set; } = [];

    public List<WalletTransaction> Transactions { get; set; } = [];
}

// One line of the wallet's audit trail. Rows are only ever added: a database trigger
// rejects any UPDATE or DELETE on the Transactions table.
public class WalletTransaction
{
    public long Id { get; set; }

    public int WalletId { get; set; }

    public Wallet Wallet { get; set; } = null!;

    public TransactionType Type { get; set; }

    // Positive for chips in, negative for chips out.
    public int Amount { get; set; }

    public int BalanceAfter { get; set; }

    public long? GameRoundId { get; set; }

    public GameRound? GameRound { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class GameRound
{
    public long Id { get; set; }

    public int PlayerId { get; set; }

    public Player Player { get; set; } = null!;

    public int Bet { get; set; }

    public RoundState State { get; set; }

    // Cards are saved as comma-separated codes, e.g. "AS,10H". The undealt deck is saved
    // too, so the round can carry on in the next request and be audited afterwards.
    public string PlayerCards { get; set; } = "";

    public string DealerCards { get; set; } = "";

    public string DeckCards { get; set; } = "";

    public RoundResult? Result { get; set; }

    public int? Payout { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public byte[] RowVersion { get; set; } = [];
}

public class LeaderboardEntry
{
    public int PlayerId { get; set; }

    public Player Player { get; set; } = null!;

    public int RoundsPlayed { get; set; }

    public int RoundsWon { get; set; }

    public int NetWinnings { get; set; }

    public int BiggestWin { get; set; }

    public DateTime UpdatedAt { get; set; }
}
