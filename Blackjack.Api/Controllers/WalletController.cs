using Blackjack.Api.Auth;
using Blackjack.Api.Services;
using Blackjack.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Blackjack.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/wallet")]
public class WalletController(WalletService wallets) : ControllerBase
{
    // Balance plus the most recent audit-trail entries.
    [HttpGet]
    public Task<WalletDto> Get() => wallets.GetAsync(User.GetPlayerId());

    [HttpPost("refill")]
    public Task<WalletDto> Refill() => wallets.RefillAsync(User.GetPlayerId());
}
