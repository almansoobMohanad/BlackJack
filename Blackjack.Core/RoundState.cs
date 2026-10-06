namespace Blackjack.Core;

// A round moves through these in order: PlayerTurn -> DealerTurn -> RoundOver.
// It can jump straight to RoundOver (a blackjack on the deal, or the player busting).
public enum RoundState
{
    PlayerTurn,
    DealerTurn,
    RoundOver
}
