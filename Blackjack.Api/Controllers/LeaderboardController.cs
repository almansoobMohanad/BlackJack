using Blackjack.Api.Data;
using Blackjack.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Blackjack.Api.Controllers;

[ApiController]
[Route("api/leaderboard")]
public class LeaderboardController(BlackjackDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<LeaderboardEntryDto>> Get(int top = 10)
    {
        top = Math.Clamp(top, 1, 50);

        var rows = await db.Leaderboard
            .AsNoTracking()
            .Where(l => l.RoundsPlayed > 0)
            .OrderByDescending(l => l.NetWinnings)
            .ThenBy(l => l.Player.Name)
            .Take(top)
            .Select(l => new { l.Player.Name, l.RoundsPlayed, l.RoundsWon, l.NetWinnings, l.BiggestWin })
            .ToListAsync();

        return rows
            .Select((row, index) => new LeaderboardEntryDto(
                index + 1, row.Name, row.RoundsPlayed, row.RoundsWon, row.NetWinnings, row.BiggestWin))
            .ToList();
    }
}
