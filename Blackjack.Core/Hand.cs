namespace Blackjack.Core;

public class Hand
{
    private readonly List<Card> _cards = new();

    public IReadOnlyList<Card> Cards => _cards;

    public void Add(Card card) => _cards.Add(card);

    public void Clear() => _cards.Clear();

    // Total with every ace counted as 1.
    private int HardTotal => _cards.Sum(card => card.Value);

    private bool HasAce => _cards.Any(card => card.Rank == Rank.Ace);

    // A hand is "soft" when one ace can count as 11 (adding 10) without going over 21.
    // Only one ace can ever count as 11, because two would already make 22.
    public bool IsSoft => HasAce && HardTotal + 10 <= 21;

    public int Total => IsSoft ? HardTotal + 10 : HardTotal;

    public bool IsBust => Total > 21;

    // Blackjack is 21 with the first two cards only (an ace plus a ten-value card).
    public bool IsBlackjack => _cards.Count == 2 && Total == 21;
}
