using Blackjack.Api.Auth;
using Blackjack.Api.Services;
using Blackjack.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Blackjack.Api.Controllers;

[ApiController]
[Route("api/players")]
public class PlayersController(PlayerService players) : ControllerBase
{
    // Sign up. The response holds the player's token; send it as "Authorization: Bearer <token>".
    [HttpPost]
    public async Task<ActionResult<PlayerSessionDto>> Create(CreatePlayerRequest request)
    {
        var session = await players.CreateAsync(request.Name);
        return Created("/api/players/me", session);
    }

    [Authorize]
    [HttpGet("me")]
    public Task<ProfileDto> Me() => players.GetProfileAsync(User.GetPlayerId());
}
