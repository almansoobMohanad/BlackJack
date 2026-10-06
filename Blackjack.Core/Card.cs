namespace Blackjack.Core;

public record Card(Rank Rank, Suit Suit)
{
    // Points this card is worth. Aces count as 1 here; Hand decides when an ace is worth 11.
    public int Value => Rank switch
    {
        Rank.Ace => 1,
        Rank.Jack or Rank.Queen or Rank.King => 10,
        _ => (int)Rank
    };

    public string RankLabel => Rank switch
    {
        Rank.Ace => "A",
        Rank.King => "K",
        Rank.Queen => "Q",
        Rank.Jack => "J",
        _ => ((int)Rank).ToString()
    };

    // Short form used when saving cards, e.g. "AS" (ace of spades) or "10H" (ten of hearts).
    public string Code => RankLabel + Suit.ToString()[0];

    public static Card Parse(string code)
    {
        if (code.Length is < 2 or > 3)
        {
            throw new FormatException($"'{code}' is not a card code.");
        }

        var suit = code[^1] switch
        {
            'C' => Suit.Clubs,
            'D' => Suit.Diamonds,
            'H' => Suit.Hearts,
            'S' => Suit.Spades,
            _ => throw new FormatException($"'{code}' has an unknown suit.")
        };

        var rank = code[..^1] switch
        {
            "A" => Rank.Ace,
            "K" => Rank.King,
            "Q" => Rank.Queen,
            "J" => Rank.Jack,
            var number when int.TryParse(number, out int n) && n is >= 2 and <= 10 => (Rank)n,
            _ => throw new FormatException($"'{code}' has an unknown rank.")
        };

        return new Card(rank, suit);
    }

    public override string ToString() => Code;
}
