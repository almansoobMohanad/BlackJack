using System.Security.Cryptography;

namespace Blackjack.Core;

public class Deck
{
    private readonly List<Card> _cards;
    private readonly Random? _random;

    // A standard 52-card deck, shuffled.
    // Pass a seeded Random for a repeatable shuffle in tests; otherwise the shuffle uses
    // a cryptographic random number generator, so nobody can predict the order.
    public Deck(Random? random = null) : this(StandardCards(), random)
    {
        Shuffle();
    }

    // A deck that deals exactly these cards in this order (first card is drawn first).
    // Used to restore a saved deck, and to set up exact hands in tests.
    public Deck(IEnumerable<Card> cardsInDrawOrder, Random? random = null)
    {
        _cards = cardsInDrawOrder.ToList();
        _random = random;
    }

    // The cards still to be dealt, in draw order.
    public IReadOnlyList<Card> Cards => _cards;

    public int Remaining => _cards.Count;

    // Fisher-Yates shuffle: walk backwards through the deck, swapping each card
    // with a randomly chosen card at or before it.
    public void Shuffle()
    {
        for (int i = _cards.Count - 1; i > 0; i--)
        {
            int j = _random?.Next(i + 1) ?? RandomNumberGenerator.GetInt32(i + 1);
            (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
        }
    }

    public Card Draw()
    {
        if (_cards.Count == 0)
        {
            throw new InvalidOperationException("The deck is empty.");
        }

        var card = _cards[0];
        _cards.RemoveAt(0);
        return card;
    }

    private static IEnumerable<Card> StandardCards() =>
        from suit in Enum.GetValues<Suit>()
        from rank in Enum.GetValues<Rank>()
        select new Card(rank, suit);
}
