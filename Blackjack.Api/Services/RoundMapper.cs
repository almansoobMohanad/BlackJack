using Blackjack.Api.Data;
using Blackjack.Contracts;
using Blackjack.Core;

namespace Blackjack.Api.Services;

// Converts between the game rules (Core), database rows (Data) and what the browser sees (Contracts).
public static class RoundMapper
{
    public static string ToCodes(IEnumerable<Card> cards) => string.Join(',', cards.Select(card => card.Code));

    public static IEnumerable<Card> FromCodes(string codes) =>
        codes.Length == 0 ? [] : codes.Split(',').Select(Card.Parse);

    public static BlackjackRound Restore(GameRound entity) =>
        BlackjackRound.Restore(
            entity.Bet,
            entity.State,
            FromCodes(entity.PlayerCards),
            FromCodes(entity.DealerCards),
            FromCodes(entity.DeckCards));

    public static void CopyCards(BlackjackRound round, GameRound entity)
    {
        entity.State = round.State;
        entity.PlayerCards = ToCodes(round.Player.Cards);
        entity.DealerCards = ToCodes(round.Dealer.Cards);
        entity.DeckCards = ToCodes(round.DeckCards);
    }

    // This is where the anti-cheat line is drawn: while the player is still deciding,
    // the dealer's hole card and total are left out of the response entirely.
    public static RoundDto ToDto(GameRound entity, BlackjackRound round, int balance)
    {
        bool isOver = round.State == RoundState.RoundOver;
        bool hideHoleCard = round.State == RoundState.PlayerTurn;

        return new RoundDto(
            entity.Id,
            isOver ? RoundStatus.RoundOver : RoundStatus.PlayerTurn,
            round.Bet,
            ToDto(round.Player, hideSecondCard: false),
            ToDto(round.Dealer, hideHoleCard),
            isOver ? ToOutcome(round.Result!.Value) : null,
            isOver ? round.Winnings : null,
            balance);
    }

    private static HandDto ToDto(Hand hand, bool hideSecondCard)
    {
        var cards = hand.Cards
            .Select((card, index) => hideSecondCard && index == 1 ? null : ToDto(card))
            .ToList();

        return hideSecondCard
            ? new HandDto(cards, Total: null, IsSoft: false, IsBlackjack: false, IsBust: false)
            : new HandDto(cards, hand.Total, hand.IsSoft, hand.IsBlackjack, hand.IsBust);
    }

    private static CardDto ToDto(Card card) => new(card.RankLabel, card.Suit switch
    {
        Suit.Clubs => CardSuit.Clubs,
        Suit.Diamonds => CardSuit.Diamonds,
        Suit.Hearts => CardSuit.Hearts,
        _ => CardSuit.Spades
    });

    private static RoundOutcome ToOutcome(RoundResult result) => result switch
    {
        RoundResult.PlayerBlackjack => RoundOutcome.PlayerBlackjack,
        RoundResult.PlayerWin => RoundOutcome.PlayerWin,
        RoundResult.Push => RoundOutcome.Push,
        _ => RoundOutcome.DealerWin
    };
}
