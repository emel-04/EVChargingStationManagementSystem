using EVChargingStation.Shared.Models;
using EVChargingStation.PaymentService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EVChargingStation.PaymentService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WalletController : ControllerBase
{
    private readonly IWalletService _walletService;
    private readonly ILogger<WalletController> _logger;

    public WalletController(IWalletService walletService, ILogger<WalletController> logger)
    {
        _walletService = walletService;
        _logger = logger;
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<Wallet>> GetUserWallet(int userId)
    {
        // Check if user is accessing their own wallet or is admin/staff
        var currentUserId = GetCurrentUserId();
        if (currentUserId != userId && !User.IsInRole("Admin") && !User.IsInRole("CSStaff"))
        {
            return Forbid();
        }

        var wallet = await _walletService.GetWalletByUserIdAsync(userId);
        if (wallet == null)
        {
            return NotFound();
        }

        return Ok(wallet);
    }

    [HttpGet("balance/{userId}")]
    public async Task<ActionResult<decimal>> GetWalletBalance(int userId)
    {
        // Check if user is accessing their own balance or is admin/staff
        var currentUserId = GetCurrentUserId();
        if (currentUserId != userId && !User.IsInRole("Admin") && !User.IsInRole("CSStaff"))
        {
            return Forbid();
        }

        var balance = await _walletService.GetWalletBalanceAsync(userId);
        return Ok(balance);
    }

    [HttpGet("transactions/{userId}")]
    public async Task<ActionResult<IEnumerable<WalletTransaction>>> GetWalletTransactions(int userId)
    {
        // Check if user is accessing their own transactions or is admin/staff
        var currentUserId = GetCurrentUserId();
        if (currentUserId != userId && !User.IsInRole("Admin") && !User.IsInRole("CSStaff"))
        {
            return Forbid();
        }

        var transactions = await _walletService.GetWalletTransactionsAsync(userId);
        return Ok(transactions);
    }

    [HttpGet("transactions/{userId}/date-range")]
    public async Task<ActionResult<IEnumerable<WalletTransaction>>> GetWalletTransactionsByDateRange(
        int userId, 
        [FromQuery] DateTime fromDate, 
        [FromQuery] DateTime toDate)
    {
        // Check if user is accessing their own transactions or is admin/staff
        var currentUserId = GetCurrentUserId();
        if (currentUserId != userId && !User.IsInRole("Admin") && !User.IsInRole("CSStaff"))
        {
            return Forbid();
        }

        var transactions = await _walletService.GetWalletTransactionsAsync(userId, fromDate, toDate);
        return Ok(transactions);
    }

    [HttpPost("deposit")]
    public async Task<ActionResult> DepositToWallet(DepositToWalletRequest request)
    {
        // Check if user is depositing to their own wallet or is admin
        var currentUserId = GetCurrentUserId();
        if (currentUserId != request.UserId && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        try
        {
            var result = await _walletService.AddToWalletAsync(request.UserId, request.Amount, request.Description);
            if (!result)
            {
                return BadRequest("Failed to deposit to wallet");
            }

            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("withdraw")]
    public async Task<ActionResult> WithdrawFromWallet(WithdrawFromWalletRequest request)
    {
        // Check if user is withdrawing from their own wallet or is admin
        var currentUserId = GetCurrentUserId();
        if (currentUserId != request.UserId && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        try
        {
            var result = await _walletService.DeductFromWalletAsync(request.UserId, request.Amount, request.Description);
            if (!result)
            {
                return BadRequest("Failed to withdraw from wallet");
            }

            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("transfer")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> TransferBetweenWallets(TransferBetweenWalletsRequest request)
    {
        try
        {
            var result = await _walletService.TransferBetweenWalletsAsync(
                request.FromUserId, 
                request.ToUserId, 
                request.Amount, 
                request.Description);

            if (!result)
            {
                return BadRequest("Failed to transfer between wallets");
            }

            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }
}

public class DepositToWalletRequest
{
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
}

public class WithdrawFromWalletRequest
{
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
}

public class TransferBetweenWalletsRequest
{
    public int FromUserId { get; set; }
    public int ToUserId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
}






