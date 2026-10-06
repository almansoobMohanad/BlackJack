using Blackjack.Api.Auth;
using Blackjack.Api.Services;
using Blackjack.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Blackjack.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/rounds")]
public class RoundsController(GameService games) : ControllerBase
{
    // Place a bet and deal a new round.
    [HttpPost]
    public Task<RoundDto> Deal(DealRequest request) => games.DealAsync(User.GetPlayerId(), request.Bet);

    // The round still in progress, or 204 No Content if there isn't one.
    [HttpGet("current")]
    public async Task<ActionResult<RoundDto>> Current()
    {
        var round = await games.GetCurrentRoundAsync(User.GetPlayerId());
        return round is null ? NoContent() : round;
    }

    [HttpPost("{id:long}/hit")]
    public Task<RoundDto> Hit(long id) => games.HitAsync(User.GetPlayerId(), id);

    [HttpPost("{id:long}/stand")]
    public Task<RoundDto> Stand(long id) => games.StandAsync(User.GetPlayerId(), id);
}
