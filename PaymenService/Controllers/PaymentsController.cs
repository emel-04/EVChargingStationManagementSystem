using EVCharging.PaymentService.DTOs;
using EVCharging.PaymentService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EVCharging.PaymentService.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IWalletService _walletService;

    public PaymentsController(IPaymentService paymentService, IWalletService walletService)
    {
        _paymentService = paymentService;
        _walletService = walletService;
    }

    [HttpPost]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request)
    {
        try
        {
            var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var payment = await _paymentService.CreatePaymentAsync(userId, request);
            return CreatedAtAction(nameof(GetPaymentById), new { paymentId = payment.PaymentId }, payment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{paymentId}")]
    public async Task<IActionResult> GetPaymentById(long paymentId)
    {
        var payment = await _paymentService.GetPaymentByIdAsync(paymentId);
        if (payment == null)
            return NotFound();

        return Ok(payment);
    }

    [HttpGet("code/{paymentCode}")]
    public async Task<IActionResult> GetPaymentByCode(string paymentCode)
    {
        var payment = await _paymentService.GetPaymentByCodeAsync(paymentCode);
        if (payment == null)
            return NotFound();

        return Ok(payment);
    }

    [HttpGet("user/my-payments")]
    public async Task<IActionResult> GetMyPayments([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var payments = await _paymentService.GetUserPaymentsAsync(userId, pageNumber, pageSize);
        return Ok(payments);
    }

    [HttpPut("{paymentId}/complete")]
    public async Task<IActionResult> CompletePayment(long paymentId, [FromBody] CompletePaymentRequest request)
    {
        var success = await _paymentService.CompletePaymentAsync(paymentId, request.TransactionId);
        if (!success)
            return BadRequest(new { message = "Cannot complete payment" });

        return Ok(new { message = "Payment completed successfully" });
    }

    [HttpPut("{paymentId}/refund")]
    public async Task<IActionResult> RefundPayment(long paymentId)
    {
        var success = await _paymentService.RefundPaymentAsync(paymentId);
        if (!success)
            return BadRequest(new { message = "Cannot refund payment" });

        return Ok(new { message = "Payment refunded successfully" });
    }
}

public class CompletePaymentRequest
{
    public string TransactionId { get; set; } = string.Empty;
}