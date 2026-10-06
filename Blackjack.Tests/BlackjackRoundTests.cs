using Blackjack.Core;

namespace Blackjack.Tests;

public class BlackjackRoundTests
{
    // Deals a $10 round from a deck holding these ranks in order:
    // player, dealer, player, dealer, then any extra cards for hits.
    private static BlackjackRound DealWith(params Rank[] ranks) =>
        BlackjackRound.Deal(new Deck(ranks.Select(rank => new Card(rank, Suit.Hearts))), bet: 10);

    [Fact]
    public void Deal_GivesTwoCardsEachAndStartsPlayerTurn()
    {
        var round = DealWith(Rank.Ten, Rank.Nine, Rank.Seven, Rank.Eight);

        Assert.Equal(RoundState.PlayerTurn, round.State);
        Assert.Equal(17, round.Player.Total);
        Assert.Equal(17, round.Dealer.Total);
        Assert.Null(round.Result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Deal_RejectsNonPositiveBets(int bet)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BlackjackRound.Deal(new Deck(), bet));
    }

    [Fact]
    public void PlayerBlackjack_PaysThreeToTwo()
    {
        var round = DealWith(Rank.Ace, Rank.Nine, Rank.King, Rank.Seven);

        Assert.Equal(RoundState.RoundOver, round.State);
        Assert.Equal(RoundResult.PlayerBlackjack, round.Result);
        Assert.Equal(25, round.Payout);
        Assert.Equal(15, round.Winnings);
    }

    [Fact]
    public void DealerBlackjack_PlayerLoses()
    {
        var round = DealWith(Rank.Nine, Rank.Ace, Rank.Nine, Rank.King);

        Assert.Equal(RoundResult.DealerWin, round.Result);
        Assert.Equal(0, round.Payout);
        Assert.Equal(-10, round.Winnings);
    }

    [Fact]
    public void BothBlackjack_IsPush()
    {
        var round = DealWith(Rank.Ace, Rank.Ace, Rank.King, Rank.Queen);

        Assert.Equal(RoundResult.Push, round.Result);
        Assert.Equal(10, round.Payout);
    }

    [Fact]
    public void PlayerBusts_LosesStraightAway()
    {
        var round = DealWith(Rank.Ten, Rank.Nine, Rank.Six, Rank.Eight, Rank.King);

        round.Hit();

        Assert.True(round.Player.IsBust);
        Assert.Equal(RoundState.RoundOver, round.State);
        Assert.Equal(RoundResult.DealerWin, round.Result);
        Assert.Equal(2, round.Dealer.Cards.Count);
    }

    [Fact]
    public void HittingTo21_StandsAutomatically()
    {
        var round = DealWith(Rank.Ten, Rank.Nine, Rank.Six, Rank.Eight, Rank.Five);

        round.Hit();

        Assert.Equal(21, round.Player.Total);
        Assert.Equal(RoundState.DealerTurn, round.State);
    }

    [Fact]
    public void Dealer_HitsOnSixteen()
    {
        // Player 19, dealer 16, dealer draws a 5 for 21.
        var round = DealWith(Rank.Ten, Rank.Ten, Rank.Nine, Rank.Six, Rank.Five);

        round.Stand();
        round.PlayDealerTurn();

        Assert.Equal(3, round.Dealer.Cards.Count);
        Assert.Equal(RoundResult.DealerWin, round.Result);
    }

    [Fact]
    public void Dealer_StandsOnSoft17()
    {
        // Player 19, dealer A+6 = soft 17.
        var round = DealWith(Rank.Ten, Rank.Ace, Rank.Nine, Rank.Six);

        round.Stand();
        round.PlayDealerTurn();

        Assert.Equal(2, round.Dealer.Cards.Count);
        Assert.Equal(RoundResult.PlayerWin, round.Result);
        Assert.Equal(20, round.Payout);
    }

    [Fact]
    public void DealerBusts_PlayerWins()
    {
        // Player 18, dealer 16, dealer draws a King for 26.
        var round = DealWith(Rank.Ten, Rank.Ten, Rank.Eight, Rank.Six, Rank.King);

        round.Stand();
        round.PlayDealerTurn();

        Assert.True(round.Dealer.IsBust);
        Assert.Equal(RoundResult.PlayerWin, round.Result);
    }

    [Fact]
    public void EqualTotals_IsPush()
    {
        var round = DealWith(Rank.Ten, Rank.Ten, Rank.Eight, Rank.Eight);

        round.Stand();
        round.PlayDealerTurn();

        Assert.Equal(RoundResult.Push, round.Result);
    }

    [Fact]
    public void HigherDealerTotal_DealerWins()
    {
        var round = DealWith(Rank.Ten, Rank.Ten, Rank.Seven, Rank.Nine);

        round.Stand();
        round.PlayDealerTurn();

        Assert.Equal(RoundResult.DealerWin, round.Result);
    }

    [Fact]
    public void Hit_AfterRoundIsOver_Throws()
    {
        var round = DealWith(Rank.Ace, Rank.Nine, Rank.King, Rank.Seven);

        Assert.Throws<InvalidOperationException>(round.Hit);
    }

    [Fact]
    public void DealerStep_DuringPlayerTurn_Throws()
    {
        var round = DealWith(Rank.Ten, Rank.Ten, Rank.Seven, Rank.Nine);

        Assert.Throws<InvalidOperationException>(round.DealerStep);
    }

    [Fact]
    public void SavedRound_RestoresAndPlaysOnIdentically()
    {
        var original = DealWith(Rank.Ten, Rank.Ten, Rank.Two, Rank.Six, Rank.Five, Rank.Four);
        original.Hit();

        var restored = BlackjackRound.Restore(
            original.Bet,
            original.State,
            original.Player.Cards,
            original.Dealer.Cards,
            original.DeckCards);

        Assert.Equal(original.Player.Total, restored.Player.Total);
        Assert.Equal(original.Dealer.Total, restored.Dealer.Total);

        // Player 17 stands; dealer 16 draws the 4 for 20.
        restored.Stand();
        restored.PlayDealerTurn();

        Assert.Equal(20, restored.Dealer.Total);
        Assert.Equal(RoundResult.DealerWin, restored.Result);
    }
}
