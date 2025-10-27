using EVChargingStation.Shared.Models;
using EVChargingStation.BookingService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EVChargingStation.BookingService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly ILogger<BookingController> _logger;

    public BookingController(IBookingService bookingService, ILogger<BookingController> logger)
    {
        _bookingService = bookingService;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Booking>> GetBooking(int id)
    {
        var booking = await _bookingService.GetBookingByIdAsync(id);
        if (booking == null)
        {
            return NotFound();
        }

        // Check if user owns the booking or is admin/staff
        var userId = GetCurrentUserId();
        if (userId != booking.UserId && !User.IsInRole("Admin") && !User.IsInRole("CSStaff"))
        {
            return Forbid();
        }

        return Ok(booking);
    }

    [HttpGet("number/{bookingNumber}")]
    public async Task<ActionResult<Booking>> GetBookingByNumber(string bookingNumber)
    {
        var booking = await _bookingService.GetBookingByNumberAsync(bookingNumber);
        if (booking == null)
        {
            return NotFound();
        }

        // Check if user owns the booking or is admin/staff
        var userId = GetCurrentUserId();
        if (userId != booking.UserId && !User.IsInRole("Admin") && !User.IsInRole("CSStaff"))
        {
            return Forbid();
        }

        return Ok(booking);
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<Booking>>> GetUserBookings(int userId)
    {
        // Check if user is accessing their own bookings or is admin/staff
        var currentUserId = GetCurrentUserId();
        if (currentUserId != userId && !User.IsInRole("Admin") && !User.IsInRole("CSStaff"))
        {
            return Forbid();
        }

        var bookings = await _bookingService.GetBookingsByUserIdAsync(userId);
        return Ok(bookings);
    }

    [HttpGet("charging-point/{chargingPointId}")]
    [Authorize(Roles = "Admin,CSStaff")]
    public async Task<ActionResult<IEnumerable<Booking>>> GetChargingPointBookings(int chargingPointId)
    {
        var bookings = await _bookingService.GetBookingsByChargingPointIdAsync(chargingPointId);
        return Ok(bookings);
    }

    [HttpGet("status/{status}")]
    [Authorize(Roles = "Admin,CSStaff")]
    public async Task<ActionResult<IEnumerable<Booking>>> GetBookingsByStatus(BookingStatus status)
    {
        var bookings = await _bookingService.GetBookingsByStatusAsync(status);
        return Ok(bookings);
    }

    [HttpGet("active/user/{userId}")]
    public async Task<ActionResult<Booking>> GetActiveUserBooking(int userId)
    {
        // Check if user is accessing their own active booking or is admin/staff
        var currentUserId = GetCurrentUserId();
        if (currentUserId != userId && !User.IsInRole("Admin") && !User.IsInRole("CSStaff"))
        {
            return Forbid();
        }

        var booking = await _bookingService.GetActiveBookingByUserIdAsync(userId);
        if (booking == null)
        {
            return NotFound();
        }

        return Ok(booking);
    }

    [HttpGet("active/charging-point/{chargingPointId}")]
    [Authorize(Roles = "Admin,CSStaff")]
    public async Task<ActionResult<Booking>> GetActiveChargingPointBooking(int chargingPointId)
    {
        var booking = await _bookingService.GetActiveBookingByChargingPointIdAsync(chargingPointId);
        if (booking == null)
        {
            return NotFound();
        }

        return Ok(booking);
    }

    [HttpPost]
    public async Task<ActionResult<Booking>> CreateBooking(CreateBookingRequest request)
    {
        // Check if user is creating booking for themselves or is admin
        var currentUserId = GetCurrentUserId();
        if (currentUserId != request.UserId && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        try
        {
            var booking = await _bookingService.CreateBookingAsync(request);
            return CreatedAtAction(nameof(GetBooking), new { id = booking.Id }, booking);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,CSStaff")]
    public async Task<ActionResult<Booking>> UpdateBooking(int id, UpdateBookingRequest request)
    {
        try
        {
            var booking = await _bookingService.UpdateBookingAsync(id, request);
            return Ok(booking);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id}/cancel")]
    public async Task<ActionResult> CancelBooking(int id)
    {
        var booking = await _bookingService.GetBookingByIdAsync(id);
        if (booking == null)
        {
            return NotFound();
        }

        // Check if user owns the booking or is admin/staff
        var currentUserId = GetCurrentUserId();
        if (currentUserId != booking.UserId && !User.IsInRole("Admin") && !User.IsInRole("CSStaff"))
        {
            return Forbid();
        }

        try
        {
            var result = await _bookingService.CancelBookingAsync(id);
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

    [HttpPost("{id}/start-charging")]
    [Authorize(Roles = "Admin,CSStaff")]
    public async Task<ActionResult> StartCharging(int id)
    {
        try
        {
            var result = await _bookingService.StartChargingAsync(id);
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

    [HttpPost("{id}/stop-charging")]
    [Authorize(Roles = "Admin,CSStaff")]
    public async Task<ActionResult> StopCharging(int id)
    {
        try
        {
            var result = await _bookingService.StopChargingAsync(id);
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






