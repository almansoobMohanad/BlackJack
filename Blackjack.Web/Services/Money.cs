namespace Blackjack.Web.Services;

public static class Money
{
    // 25 -> "+$25", -10 -> "-$10", 0 -> "$0"
    public static string Signed(int amount) => amount switch
    {
        > 0 => $"+${amount}",
        < 0 => $"-${-amount}",
        _ => "$0"
    };
}
