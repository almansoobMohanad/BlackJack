using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blackjack.Contracts;
using Blackjack.Core;

namespace Blackjack.Api.Tests;

[Collection(ApiCollection.Name)]
public class PlayerAndWalletTests(BlackjackApiFactory api)
{
    [Fact]
    public async Task SignUp_GivesStartingChips_WithAnAuditRow()
    {
        var (client, session) = await api.SignUpAsync();

        var wallet = await client.GetFromJsonAsync<WalletDto>("api/wallet");

        Assert.Equal(GameLimits.StartingBalance, session.Balance);
        Assert.Equal(GameLimits.StartingBalance, wallet!.Balance);
        var entry = Assert.Single(wallet.RecentTransactions);
        Assert.Equal(TransactionType.SignUpBonus, entry.Type);
        Assert.Equal(GameLimits.StartingBalance, entry.Amount);
        Assert.Equal(GameLimits.StartingBalance, entry.BalanceAfter);
    }

    [Fact]
    public async Task SignUp_WithTakenName_IsRejected()
    {
        var (_, session) = await api.SignUpAsync();

        var response = await api.CreateClient().PostAsJsonAsync("api/players", new CreatePlayerRequest(session.Name));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("<script>")]
    [InlineData("a-name-that-is-far-too-long")]
    public async Task SignUp_WithBadName_IsRejected(string name)
    {
        var response = await api.CreateClient().PostAsJsonAsync("api/players", new CreatePlayerRequest(name));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Requests_WithoutAValidToken_AreRejected()
    {
        var anonymous = api.CreateClient();
        var forged = api.CreateClient();
        forged.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-real-token");

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("api/wallet")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await forged.GetAsync("api/wallet")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await forged.PostAsJsonAsync("api/rounds", new DealRequest(10))).StatusCode);
    }

    [Fact]
    public async Task Refill_IsOnlyAllowedWhenBroke()
    {
        var (client, _) = await api.SignUpAsync();

        var early = await client.PostAsync("api/wallet/refill", null);
        Assert.Equal(HttpStatusCode.BadRequest, early.StatusCode);

        // Bet everything on 17 against the dealer's 19.
        api.Decks.Next(Rank.Ten, Rank.Ten, Rank.Seven, Rank.Nine);
        var round = await (await client.PostAsJsonAsync("api/rounds", new DealRequest(GameLimits.StartingBalance)))
            .Content.ReadFromJsonAsync<RoundDto>();
        var lost = await (await client.PostAsync($"api/rounds/{round!.Id}/stand", null)).Content.ReadFromJsonAsync<RoundDto>();
        Assert.Equal(0, lost!.Balance);

        var refill = await client.PostAsync("api/wallet/refill", null);
        var wallet = await refill.Content.ReadFromJsonAsync<WalletDto>();

        Assert.Equal(HttpStatusCode.OK, refill.StatusCode);
        Assert.Equal(GameLimits.StartingBalance, wallet!.Balance);
        Assert.Equal(TransactionType.Refill, wallet.RecentTransactions[0].Type);
    }

    [Fact]
    public async Task Leaderboard_RanksPlayersByNetWinnings()
    {
        var (winner, winnerSession) = await api.SignUpAsync();
        api.Decks.Next(Rank.Ace, Rank.Nine, Rank.King, Rank.Seven); // blackjack: +150 on a 100 bet
        await winner.PostAsJsonAsync("api/rounds", new DealRequest(100));

        var board = (await api.CreateClient().GetFromJsonAsync<List<LeaderboardEntryDto>>("api/leaderboard?top=50"))!;

        var entry = Assert.Single(board, e => e.Name == winnerSession.Name);
        Assert.Equal(150, entry.NetWinnings);
        Assert.Equal(1, entry.RoundsWon);
        Assert.Equal(150, entry.BiggestWin);
        Assert.Equal(board.OrderByDescending(e => e.NetWinnings).Select(e => e.Name), board.Select(e => e.Name));
    }
}
