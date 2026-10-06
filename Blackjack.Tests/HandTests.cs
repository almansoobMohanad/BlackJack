using Blackjack.Core;

namespace Blackjack.Tests;

public class HandTests
{
    private static Hand HandOf(params Rank[] ranks)
    {
        var hand = new Hand();
        foreach (var rank in ranks)
        {
            hand.Add(new Card(rank, Suit.Spades));
        }
        return hand;
    }

    [Theory]
    [InlineData(new[] { Rank.Two, Rank.Three }, 5, false)]
    [InlineData(new[] { Rank.King, Rank.Queen }, 20, false)]
    [InlineData(new[] { Rank.Ace, Rank.Six }, 17, true)]
    [InlineData(new[] { Rank.Ace, Rank.Six, Rank.Ten }, 17, false)]
    [InlineData(new[] { Rank.Ace, Rank.Ace }, 12, true)]
    [InlineData(new[] { Rank.Ace, Rank.Ace, Rank.Nine }, 21, true)]
    [InlineData(new[] { Rank.Ace, Rank.Ace, Rank.Ace, Rank.Ace }, 14, true)]
    [InlineData(new[] { Rank.King, Rank.Queen, Rank.Two }, 22, false)]
    public void Total_CountsAcesAsOneOrEleven(Rank[] ranks, int expectedTotal, bool expectedSoft)
    {
        var hand = HandOf(ranks);

        Assert.Equal(expectedTotal, hand.Total);
        Assert.Equal(expectedSoft, hand.IsSoft);
    }

    [Fact]
    public void AceAndTenValueCard_IsBlackjack()
    {
        Assert.True(HandOf(Rank.Ace, Rank.King).IsBlackjack);
    }

    [Fact]
    public void TwentyOneWithThreeCards_IsNotBlackjack()
    {
        var hand = HandOf(Rank.Seven, Rank.Seven, Rank.Seven);

        Assert.Equal(21, hand.Total);
        Assert.False(hand.IsBlackjack);
    }

    [Fact]
    public void OverTwentyOne_IsBust()
    {
        Assert.True(HandOf(Rank.King, Rank.Queen, Rank.Two).IsBust);
        Assert.False(HandOf(Rank.King, Rank.Ace).IsBust);
    }
}
