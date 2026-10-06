using System.Text.Json.Serialization;

namespace Blackjack.Contracts;

// These mirror the Core enums but belong to the API's public shape, so the browser
// never needs a reference to the game rules. They're sent as text, e.g. "PlayerTurn".

[JsonConverter(typeof(JsonStringEnumConverter<CardSuit>))]
public enum CardSuit
{
    Clubs,
    Diamonds,
    Hearts,
    Spades
}

[JsonConverter(typeof(JsonStringEnumConverter<RoundStatus>))]
public enum RoundStatus
{
    PlayerTurn,
    RoundOver
}

[JsonConverter(typeof(JsonStringEnumConverter<RoundOutcome>))]
public enum RoundOutcome
{
    PlayerBlackjack,
    PlayerWin,
    DealerWin,
    Push
}

[JsonConverter(typeof(JsonStringEnumConverter<TransactionType>))]
public enum TransactionType
{
    SignUpBonus,
    Bet,
    Payout,
    Refill
}
