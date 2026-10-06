using System.Net;
using System.Net.Http.Json;
using Blackjack.Contracts;
using Blackjack.Core;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Blackjack.Api.Tests;

[Collection(ApiCollection.Name)]
public class RoundTests(BlackjackApiFactory api)
{
    private static async Task<RoundDto> ReadRoundAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<RoundDto>())!;
    }

    [Fact]
    public async Task Deal_NeverRevealsTheDealersHoleCard()
    {
        var (client, _) = await api.SignUpAsync();
        api.Decks.Next(Rank.Ten, Rank.Nine, Rank.Seven, Rank.Eight);

        var response = await client.PostAsJsonAsync("api/rounds", new DealRequest(10));
        string rawJson = await response.Content.ReadAsStringAsync();
        var round = await ReadRoundAsync(response);

        Assert.Equal(RoundStatus.PlayerTurn, round.Status);
        Assert.Equal("9", round.Dealer.Cards[0]!.Rank);
        Assert.Null(round.Dealer.Cards[1]);
        Assert.Null(round.Dealer.Total);
        Assert.Equal(17, round.Player.Total);
        // The hidden card (the 8) must not appear anywhere in what the browser receives.
        Assert.DoesNotContain("\"8\"", rawJson);
    }

    [Fact]
    public async Task Deal_TakesTheBet_WithAnAuditRowLinkedToTheRound()
    {
        var (client, _) = await api.SignUpAsync();
        api.Decks.Next(Rank.Ten, Rank.Nine, Rank.Seven, Rank.Eight);

        var round = await ReadRoundAsync(await client.PostAsJsonAsync("api/rounds", new DealRequest(10)));
        var wallet = await client.GetFromJsonAsync<WalletDto>("api/wallet");

        Assert.Equal(490, round.Balance);
        var bet = wallet!.RecentTransactions[0];
        Assert.Equal(TransactionType.Bet, bet.Type);
        Assert.Equal(-10, bet.Amount);
        Assert.Equal(490, bet.BalanceAfter);
        Assert.Equal(round.Id, bet.GameRoundId);
    }

    [Fact]
    public async Task WinningRound_PaysOut()
    {
        var (client, _) = await api.SignUpAsync();
        // Player 18, dealer 16 then draws a King and busts.
        api.Decks.Next(Rank.Ten, Rank.Ten, Rank.Eight, Rank.Six, Rank.King);

        var dealt = await ReadRoundAsync(await client.PostAsJsonAsync("api/rounds", new DealRequest(10)));
        var finished = await ReadRoundAsync(await client.PostAsync($"api/rounds/{dealt.Id}/stand", null));
        var wallet = await client.GetFromJsonAsync<WalletDto>("api/wallet");

        Assert.Equal(RoundOutcome.PlayerWin, finished.Outcome);
        Assert.True(finished.Dealer.IsBust);
        Assert.Equal(3, finished.Dealer.Cards.Count);
        Assert.Equal(10, finished.Winnings);
        Assert.Equal(510, finished.Balance);
        Assert.Equal(
            [TransactionType.Payout, TransactionType.Bet, TransactionType.SignUpBonus],
            wallet!.RecentTransactions.Select(t => t.Type));
    }

    [Fact]
    public async Task Blackjack_PaysThreeToTwo_StraightAway()
    {
        var (client, _) = await api.SignUpAsync();
        api.Decks.Next(Rank.Ace, Rank.Nine, Rank.King, Rank.Seven);

        var round = await ReadRoundAsync(await client.PostAsJsonAsync("api/rounds", new DealRequest(10)));

        Assert.Equal(RoundStatus.RoundOver, round.Status);
        Assert.Equal(RoundOutcome.PlayerBlackjack, round.Outcome);
        Assert.Equal(515, round.Balance);
    }

    [Fact]
    public async Task Busting_LosesTheBet_WithNoPayoutRow()
    {
        var (client, _) = await api.SignUpAsync();
        api.Decks.Next(Rank.Ten, Rank.Nine, Rank.Six, Rank.Eight, Rank.King);

        var dealt = await ReadRoundAsync(await client.PostAsJsonAsync("api/rounds", new DealRequest(10)));
        var bust = await ReadRoundAsync(await client.PostAsync($"api/rounds/{dealt.Id}/hit", null));
        var wallet = await client.GetFromJsonAsync<WalletDto>("api/wallet");

        Assert.Equal(RoundOutcome.DealerWin, bust.Outcome);
        Assert.True(bust.Player.IsBust);
        Assert.Equal(490, bust.Balance);
        Assert.DoesNotContain(wallet!.RecentTransactions, t => t.Type == TransactionType.Payout);
    }

    [Fact]
    public async Task BettingMoreThanTheBalance_IsRejected_AndChangesNothing()
    {
        var (client, _) = await api.SignUpAsync();

        var response = await client.PostAsJsonAsync("api/rounds", new DealRequest(GameLimits.StartingBalance + 1));
        var wallet = await client.GetFromJsonAsync<WalletDto>("api/wallet");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(GameLimits.StartingBalance, wallet!.Balance);
        Assert.Single(wallet.RecentTransactions);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public async Task NonPositiveBets_AreRejected(int bet)
    {
        var (client, _) = await api.SignUpAsync();

        var response = await client.PostAsJsonAsync("api/rounds", new DealRequest(bet));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DealingAgain_DuringARound_IsRejected()
    {
        var (client, _) = await api.SignUpAsync();
        api.Decks.Next(Rank.Ten, Rank.Nine, Rank.Seven, Rank.Eight);
        await client.PostAsJsonAsync("api/rounds", new DealRequest(10));

        var second = await client.PostAsJsonAsync("api/rounds", new DealRequest(10));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Hitting_AFinishedRound_IsRejected()
    {
        var (client, _) = await api.SignUpAsync();
        api.Decks.Next(Rank.Ace, Rank.Nine, Rank.King, Rank.Seven);
        var round = await ReadRoundAsync(await client.PostAsJsonAsync("api/rounds", new DealRequest(10)));

        var hit = await client.PostAsync($"api/rounds/{round.Id}/hit", null);

        Assert.Equal(HttpStatusCode.Conflict, hit.StatusCode);
    }

    [Fact]
    public async Task Players_CannotTouchSomeoneElsesRound()
    {
        var (owner, _) = await api.SignUpAsync();
        var (intruder, _) = await api.SignUpAsync();
        api.Decks.Next(Rank.Ten, Rank.Nine, Rank.Seven, Rank.Eight);
        var round = await ReadRoundAsync(await owner.PostAsJsonAsync("api/rounds", new DealRequest(10)));

        var response = await intruder.PostAsync($"api/rounds/{round.Id}/stand", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CurrentRound_SurvivesAPageReload()
    {
        var (client, _) = await api.SignUpAsync();
        api.Decks.Next(Rank.Ten, Rank.Nine, Rank.Seven, Rank.Eight);
        var dealt = await ReadRoundAsync(await client.PostAsJsonAsync("api/rounds", new DealRequest(10)));

        var profile = await client.GetFromJsonAsync<ProfileDto>("api/players/me");

        Assert.Equal(dealt.Id, profile!.CurrentRound!.Id);
        Assert.Null(profile.CurrentRound.Dealer.Cards[1]);
    }

    [Fact]
    public async Task SimultaneousDeals_OnlyOneGoesThrough()
    {
        var (client, session) = await api.SignUpAsync();

        // Five "Deal" clicks arriving at the same moment.
        var responses = await Task.WhenAll(Enumerable.Range(0, 5)
            .Select(_ => client.PostAsJsonAsync("api/rounds", new DealRequest(100))));

        await using var db = api.CreateDbContext();
        var wallet = await db.Wallets.SingleAsync(w => w.PlayerId == session.PlayerId);
        var ledger = await db.Transactions.Where(t => t.WalletId == wallet.Id).OrderBy(t => t.Id).ToListAsync();

        // A deal that lands a blackjack finishes immediately, so the next deal is allowed.
        // Whatever happens, every accepted bet must have exactly one audit row and the balance must add up.
        Assert.All(responses, r => Assert.Contains(r.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Conflict }));
        Assert.Contains(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Equal(
            responses.Count(r => r.StatusCode == HttpStatusCode.OK),
            ledger.Count(t => t.Type == TransactionType.Bet));
        Assert.Equal(wallet.Balance, ledger.Sum(t => t.Amount));
    }

    [Fact]
    public async Task AuditTrail_AlwaysAddsUpToTheBalance()
    {
        var (client, session) = await api.SignUpAsync();
        for (int i = 0; i < 6; i++)
        {
            var round = await ReadRoundAsync(await client.PostAsJsonAsync("api/rounds", new DealRequest(25)));
            if (round.Status == RoundStatus.PlayerTurn)
            {
                await client.PostAsync($"api/rounds/{round.Id}/stand", null);
            }
        }

        await using var db = api.CreateDbContext();
        var wallet = await db.Wallets.SingleAsync(w => w.PlayerId == session.PlayerId);
        var ledger = await db.Transactions.Where(t => t.WalletId == wallet.Id).OrderBy(t => t.Id).ToListAsync();

        int running = 0;
        foreach (var entry in ledger)
        {
            running += entry.Amount;
            Assert.Equal(running, entry.BalanceAfter);
        }
        Assert.Equal(wallet.Balance, running);
    }

    [Fact]
    public async Task AuditTrail_CannotBeEditedOrDeleted()
    {
        var (_, session) = await api.SignUpAsync();
        await using var db = api.CreateDbContext();
        var walletId = await db.Wallets.Where(w => w.PlayerId == session.PlayerId).Select(w => w.Id).SingleAsync();
        var rows = db.Transactions.Where(t => t.WalletId == walletId);

        await Assert.ThrowsAsync<SqlException>(() => rows.ExecuteUpdateAsync(s => s.SetProperty(t => t.Amount, 1_000_000)));
        await Assert.ThrowsAsync<SqlException>(() => rows.ExecuteDeleteAsync());

        Assert.Equal(GameLimits.StartingBalance, (await rows.SingleAsync()).Amount);
    }
}
