using Blackjack.Core;

namespace Blackjack.Api.Services;

// Where new decks come from. Tests swap this out to deal known cards.
public interface IDeckFactory
{
    Deck Create();
}

// Every round gets a fresh, securely shuffled 52-card deck, so there's nothing to count
// between rounds and nothing for the client to predict.
public class ShuffledDeckFactory : IDeckFactory
{
    public Deck Create() => new();
}
