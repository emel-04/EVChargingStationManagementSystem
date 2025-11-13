using EVChargingStation.Shared.Models;
using EVChargingStation.PaymentService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EVChargingStation.PaymentService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(IPaymentService paymentService, ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Payment>> GetPayment(int id)
    {
        var payment = await _paymentService.GetPaymentByIdAsync(id);
        if (payment == null)
        {
            return NotFound();
        }

        // Check if user owns the payment or is admin/staff
        var userId = GetCurrentUserId();
        if (userId != payment.UserId && !User.IsInRole("Admin") && !User.IsInRole("CSStaff"))
        {
            return Forbid();
        }

        return Ok(payment);
    }

    [HttpGet("number/{paymentNumber}")]
    public async Task<ActionResult<Payment>> GetPaymentByNumber(string paymentNumber)
    {
        var payment = await _paymentService.GetPaymentByNumberAsync(paymentNumber);
        if (payment == null)
        {
            return NotFound();
        }

        // Check if user owns the payment or is admin/staff
        var userId = GetCurrentUserId();
        if (userId != payment.UserId && !User.IsInRole("Admin") && !User.IsInRole("CSStaff"))
        {
            return Forbid();
        }

        return Ok(payment);
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<Payment>>> GetUserPayments(int userId)
    {
        // Check if user is accessing their own payments or is admin/staff
        var currentUserId = GetCurrentUserId();
        if (currentUserId != userId && !User.IsInRole("Admin") && !User.IsInRole("CSStaff"))
        {
            return Forbid();
        }

        var payments = await _paymentService.GetPaymentsByUserIdAsync(userId);
        return Ok(payments);
    }

    [HttpGet("booking/{bookingId}")]
    public async Task<ActionResult<IEnumerable<Payment>>> GetBookingPayments(int bookingId)
    {
        var payments = await _paymentService.GetPaymentsByBookingIdAsync(bookingId);
        return Ok(payments);
    }

    [HttpGet("status/{status}")]
    [Authorize(Roles = "Admin,CSStaff")]
    public async Task<ActionResult<IEnumerable<Payment>>> GetPaymentsByStatus(PaymentStatus status)
    {
        var payments = await _paymentService.GetPaymentsByStatusAsync(status);
        return Ok(payments);
    }

    [HttpPost]
    public async Task<ActionResult<Payment>> CreatePayment(CreatePaymentRequest request)
    {
        // Check if user is creating payment for themselves or is admin
        var currentUserId = GetCurrentUserId();
        if (currentUserId != request.UserId && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        try
        {
            var payment = await _paymentService.CreatePaymentAsync(request);
            return CreatedAtAction(nameof(GetPayment), new { id = payment.Id }, payment);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id}/process")]
    [Authorize(Roles = "Admin,CSStaff")]
    public async Task<ActionResult<Payment>> ProcessPayment(int id, ProcessPaymentRequest request)
    {
        try
        {
            var payment = await _paymentService.ProcessPaymentAsync(id, request);
            return Ok(payment);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id}/refund")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> RefundPayment(int id, RefundPaymentRequest request)
    {
        try
        {
            var result = await _paymentService.RefundPaymentAsync(id, request);
            if (!result)
            {
                return NotFound();
            }

            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("booking/{bookingId}")]
    public async Task<ActionResult<Payment>> CreatePaymentForBooking(int bookingId, CreatePaymentForBookingRequest request)
    {
        try
        {
            var payment = await _paymentService.CreatePaymentForBookingAsync(bookingId, request.Amount, request.Method);
            return CreatedAtAction(nameof(GetPayment), new { id = payment.Id }, payment);
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

public class CreatePaymentForBookingRequest
{
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
}






