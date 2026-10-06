namespace Blackjack.Contracts;

// ---------- Cards and rounds ----------

// Rank is "A", "K", "Q", "J" or "2".."10".
public record CardDto(string Rank, CardSuit Suit);

// A null card is face-down. While a card is hidden the total and flags are hidden too,
// so nothing about the dealer's hole card leaks to the browser.
public record HandDto(
    IReadOnlyList<CardDto?> Cards,
    int? Total,
    bool IsSoft,
    bool IsBlackjack,
    bool IsBust);

public record RoundDto(
    long Id,
    RoundStatus Status,
    int Bet,
    HandDto Player,
    HandDto Dealer,
    RoundOutcome? Outcome,
    int? Winnings,
    int Balance);

public record DealRequest(int Bet);

// ---------- Players ----------

public record CreatePlayerRequest(string Name);

// Returned once, when a player signs up. The token is the player's key for every later request.
public record PlayerSessionDto(int PlayerId, string Name, string Token, int Balance);

public record ProfileDto(int PlayerId, string Name, int Balance, RoundDto? CurrentRound);

// ---------- Wallet ----------

public record TransactionDto(
    long Id,
    TransactionType Type,
    int Amount,
    int BalanceAfter,
    long? GameRoundId,
    DateTime CreatedAt);

public record WalletDto(int Balance, IReadOnlyList<TransactionDto> RecentTransactions);

// ---------- Leaderboard ----------

public record LeaderboardEntryDto(
    int Position,
    string Name,
    int RoundsPlayed,
    int RoundsWon,
    int NetWinnings,
    int BiggestWin);

// ---------- Rules shared by both sides ----------

public static class GameLimits
{
    public const int StartingBalance = 500;
    public const int MinBet = 1;
    public const int MaxBet = 1000;
    public const int MaxNameLength = 20;
}
