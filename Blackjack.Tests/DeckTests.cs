using Blackjack.Core;

namespace Blackjack.Tests;

public class DeckTests
{
    private static List<Card> DrawAll(Deck deck, int count) =>
        Enumerable.Range(0, count).Select(_ => deck.Draw()).ToList();

    [Fact]
    public void StandardDeck_Has52UniqueCards()
    {
        var deck = new Deck();

        var cards = DrawAll(deck, 52);

        Assert.Equal(52, cards.Distinct().Count());
        Assert.Equal(0, deck.Remaining);
    }

    [Fact]
    public void StackedDeck_DealsInTheGivenOrder()
    {
        var aceOfSpades = new Card(Rank.Ace, Suit.Spades);
        var twoOfHearts = new Card(Rank.Two, Suit.Hearts);
        var deck = new Deck([aceOfSpades, twoOfHearts]);

        Assert.Equal(aceOfSpades, deck.Draw());
        Assert.Equal(twoOfHearts, deck.Draw());
    }

    [Fact]
    public void SameSeed_GivesSameShuffle()
    {
        var first = DrawAll(new Deck(new Random(42)), 52);
        var second = DrawAll(new Deck(new Random(42)), 52);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Cards_ListsWhatIsLeftInDrawOrder()
    {
        var deck = new Deck([new Card(Rank.Ace, Suit.Spades), new Card(Rank.Two, Suit.Hearts)]);
        deck.Draw();

        Assert.Equal([new Card(Rank.Two, Suit.Hearts)], deck.Cards);
    }

    [Fact]
    public void DrawingFromEmptyDeck_Throws()
    {
        var deck = new Deck([]);

        Assert.Throws<InvalidOperationException>(deck.Draw);
    }
}
