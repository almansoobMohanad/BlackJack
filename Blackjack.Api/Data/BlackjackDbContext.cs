using Blackjack.Contracts;
using Blackjack.Core;
using Microsoft.EntityFrameworkCore;

namespace Blackjack.Api.Data;

public class BlackjackDbContext(DbContextOptions<BlackjackDbContext> options) : DbContext(options)
{
    public const string AppendOnlyTrigger = "TR_Transactions_AppendOnly";

    public DbSet<Player> Players => Set<Player>();

    public DbSet<Wallet> Wallets => Set<Wallet>();

    public DbSet<WalletTransaction> Transactions => Set<WalletTransaction>();

    public DbSet<GameRound> GameRounds => Set<GameRound>();

    public DbSet<LeaderboardEntry> Leaderboard => Set<LeaderboardEntry>();

    // Runs the work inside a database transaction: either every change it saves is kept, or none are.
    public async Task<T> InTransactionAsync<T>(Func<Task<T>> work)
    {
        await using var transaction = await Database.BeginTransactionAsync();
        T result = await work();
        await transaction.CommitAsync();
        return result;
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Player>(player =>
        {
            player.ToTable("Players");
            player.Property(p => p.Name).HasMaxLength(GameLimits.MaxNameLength);
            player.HasIndex(p => p.Name).IsUnique();
            player.Property(p => p.TokenHash).HasMaxLength(64).IsFixedLength().IsUnicode(false);
            player.HasIndex(p => p.TokenHash).IsUnique();

            player.HasOne(p => p.Wallet)
                .WithOne(w => w.Player)
                .HasForeignKey<Wallet>(w => w.PlayerId);

            player.HasOne(p => p.LeaderboardEntry)
                .WithOne(l => l.Player)
                .HasForeignKey<LeaderboardEntry>(l => l.PlayerId);
        });

        model.Entity<Wallet>(wallet =>
        {
            wallet.ToTable("Wallets", table =>
                table.HasCheckConstraint("CK_Wallets_Balance_NotNegative", "[Balance] >= 0"));
            wallet.Property(w => w.RowVersion).IsRowVersion();
        });

        model.Entity<WalletTransaction>(transaction =>
        {
            // EF Core needs to know about the trigger so it saves rows in a trigger-safe way.
            transaction.ToTable("Transactions", table => table.HasTrigger(AppendOnlyTrigger));
            transaction.Property(t => t.Type).HasConversion<string>().HasMaxLength(20).IsUnicode(false);
            transaction.HasIndex(t => new { t.WalletId, t.Id });

            // Audit rows must outlive anything they point at, so nothing cascades into them.
            transaction.HasOne(t => t.Wallet)
                .WithMany(w => w.Transactions)
                .HasForeignKey(t => t.WalletId)
                .OnDelete(DeleteBehavior.Restrict);

            transaction.HasOne(t => t.GameRound)
                .WithMany()
                .HasForeignKey(t => t.GameRoundId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        model.Entity<GameRound>(round =>
        {
            round.ToTable("GameRounds", table =>
                table.HasCheckConstraint("CK_GameRounds_Bet_Positive", "[Bet] > 0"));
            round.Property(r => r.State).HasConversion<string>().HasMaxLength(20).IsUnicode(false);
            round.Property(r => r.Result).HasConversion<string>().HasMaxLength(20).IsUnicode(false);
            round.Property(r => r.PlayerCards).HasMaxLength(64).IsUnicode(false);
            round.Property(r => r.DealerCards).HasMaxLength(64).IsUnicode(false);
            round.Property(r => r.DeckCards).HasMaxLength(210).IsUnicode(false);
            round.Property(r => r.RowVersion).IsRowVersion();

            // The database itself guarantees a player has at most one unfinished round.
            round.HasIndex(r => r.PlayerId)
                .IsUnique()
                .HasFilter($"[State] <> '{nameof(RoundState.RoundOver)}'")
                .HasDatabaseName("IX_GameRounds_OneActiveRoundPerPlayer");
            round.HasIndex(r => new { r.PlayerId, r.StartedAt });

            round.HasOne(r => r.Player)
                .WithMany(p => p.Rounds)
                .HasForeignKey(r => r.PlayerId);
        });

        model.Entity<LeaderboardEntry>(entry =>
        {
            entry.ToTable("Leaderboard");
            entry.HasKey(l => l.PlayerId);
            entry.HasIndex(l => l.NetWinnings);
        });
    }
}
