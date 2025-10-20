using EVCharging.PaymentService.DTOs;
using EVCharging.PaymentService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EVCharging.PaymentService.Controllers;

[ApiController]
[Route("api/wallets")]
[Authorize]
public class WalletsController : ControllerBase
{
    private readonly IWalletService _walletService;

    public WalletsController(IWalletService walletService)
    {
        _walletService = walletService;
    }

    [HttpGet("my-wallet")]
    public async Task<IActionResult> GetMyWallet()
    {
        var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var wallet = await _walletService.GetWalletAsync(userId);
        
        if (wallet == null)
            return NotFound();

        return Ok(wallet);
    }

    [HttpPost("topup")]
    public async Task<IActionResult> TopUpWallet([FromBody] TopUpWalletRequest request)
    {
        try
        {
            var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var wallet = await _walletService.TopUpWalletAsync(userId, request);
            return Ok(wallet);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}