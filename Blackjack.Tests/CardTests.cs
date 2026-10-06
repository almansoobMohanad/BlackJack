using Blackjack.Core;

namespace Blackjack.Tests;

public class CardTests
{
    [Theory]
    [InlineData(Rank.Ace, Suit.Spades, "AS")]
    [InlineData(Rank.Ten, Suit.Hearts, "10H")]
    [InlineData(Rank.Two, Suit.Clubs, "2C")]
    [InlineData(Rank.Queen, Suit.Diamonds, "QD")]
    public void Code_RoundTrips(Rank rank, Suit suit, string code)
    {
        var card = new Card(rank, suit);

        Assert.Equal(code, card.Code);
        Assert.Equal(card, Card.Parse(code));
    }

    [Fact]
    public void EveryCardInTheDeck_RoundTrips()
    {
        foreach (var card in new Deck().Cards)
        {
            Assert.Equal(card, Card.Parse(card.Code));
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("1S")]
    [InlineData("11S")]
    [InlineData("AX")]
    [InlineData("ZZZZ")]
    public void Parse_RejectsBadCodes(string code)
    {
        Assert.Throws<FormatException>(() => Card.Parse(code));
    }

    [Theory]
    [InlineData(Rank.Ace, 1)]
    [InlineData(Rank.Seven, 7)]
    [InlineData(Rank.Ten, 10)]
    [InlineData(Rank.King, 10)]
    public void Value_CountsFaceCardsAsTen(Rank rank, int expected)
    {
        Assert.Equal(expected, new Card(rank, Suit.Clubs).Value);
    }
}
