namespace Blackjack.Core;

// One hand of blackjack, from the deal to the result.
// It knows nothing about chips or wallets: it only works out who won and what the bet pays.
public class BlackjackRound
{
    // The dealer must keep hitting below this total, and stands on it, including on a soft 17.
    public const int DealerStandsOn = 17;

    private readonly Deck _deck;

    private BlackjackRound(Deck deck, int bet, RoundState state)
    {
        _deck = deck;
        Bet = bet;
        State = state;
    }

    public int Bet { get; }

    public RoundState State { get; private set; }

    public Hand Player { get; } = new();

    public Hand Dealer { get; } = new();

    // The cards left to deal, so an unfinished round can be saved and restored.
    public IReadOnlyList<Card> DeckCards => _deck.Cards;

    public RoundResult? Result => State == RoundState.RoundOver ? DecideResult() : null;

    // Chips handed back to the player when the round ends, including the original bet.
    // Blackjack pays 3:2 (rounded down for odd bets), a normal win pays 1:1.
    public int Payout => Result switch
    {
        RoundResult.PlayerBlackjack => Bet + Bet * 3 / 2,
        RoundResult.PlayerWin => Bet * 2,
        RoundResult.Push => Bet,
        _ => 0
    };

    // Profit (positive) or loss (negative) for the round.
    public int Winnings => Payout - Bet;

    public static BlackjackRound Deal(Deck deck, int bet)
    {
        if (bet <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bet), "Bet must be positive.");
        }

        var round = new BlackjackRound(deck, bet, RoundState.PlayerTurn);

        // Deal alternately, like at a real table: player, dealer, player, dealer.
        round.Player.Add(deck.Draw());
        round.Dealer.Add(deck.Draw());
        round.Player.Add(deck.Draw());
        round.Dealer.Add(deck.Draw());

        if (round.Player.IsBlackjack || round.Dealer.IsBlackjack)
        {
            round.State = RoundState.RoundOver;
        }

        return round;
    }

    // Rebuilds a round that was saved part-way through.
    public static BlackjackRound Restore(
        int bet,
        RoundState state,
        IEnumerable<Card> playerCards,
        IEnumerable<Card> dealerCards,
        IEnumerable<Card> deckCards)
    {
        var round = new BlackjackRound(new Deck(deckCards), bet, state);
        foreach (var card in playerCards)
        {
            round.Player.Add(card);
        }
        foreach (var card in dealerCards)
        {
            round.Dealer.Add(card);
        }
        return round;
    }

    public void Hit()
    {
        EnsureState(RoundState.PlayerTurn);

        Player.Add(_deck.Draw());

        if (Player.IsBust)
        {
            State = RoundState.RoundOver;
        }
        else if (Player.Total == 21)
        {
            // Nothing better to do on 21, so stand automatically.
            Stand();
        }
    }

    public void Stand()
    {
        EnsureState(RoundState.PlayerTurn);
        State = RoundState.DealerTurn;
    }

    // Plays one step of the dealer's turn: draws a card if the dealer is under 17,
    // then ends the round once the dealer is done.
    public void DealerStep()
    {
        EnsureState(RoundState.DealerTurn);

        if (Dealer.Total < DealerStandsOn)
        {
            Dealer.Add(_deck.Draw());
        }

        if (Dealer.Total >= DealerStandsOn)
        {
            State = RoundState.RoundOver;
        }
    }

    // Plays the whole dealer turn at once.
    public void PlayDealerTurn()
    {
        while (State == RoundState.DealerTurn)
        {
            DealerStep();
        }
    }

    // The result follows entirely from the final hands, so it never needs saving separately.
    private RoundResult DecideResult()
    {
        if (Player.IsBust)
        {
            return RoundResult.DealerWin;
        }
        if (Player.IsBlackjack && Dealer.IsBlackjack)
        {
            return RoundResult.Push;
        }
        if (Player.IsBlackjack)
        {
            return RoundResult.PlayerBlackjack;
        }
        if (Dealer.IsBlackjack)
        {
            return RoundResult.DealerWin;
        }
        if (Dealer.IsBust || Player.Total > Dealer.Total)
        {
            return RoundResult.PlayerWin;
        }
        return Player.Total == Dealer.Total ? RoundResult.Push : RoundResult.DealerWin;
    }

    private void EnsureState(RoundState expected)
    {
        if (State != expected)
        {
            throw new InvalidOperationException($"Can't do that right now (round is in {State}).");
        }
    }
}
