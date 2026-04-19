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
            return NotFound();

        var userId = GetCurrentUserId();
        if (userId != booking.UserId && !User.IsInRole("Admin") && !User.IsInRole("CSStaff"))
            return Forbid();

        return Ok(booking);
    }

  
    [HttpGet]
    [Authorize(Roles = "Admin,CSStaff")]
    public async Task<ActionResult<IEnumerable<Booking>>> GetAllBookings()
    {
        var bookings = await _bookingService.GetAllBookingsAsync();
        return Ok(bookings);
    }

    
    [HttpGet("number/{bookingNumber}")]
    public async Task<ActionResult<Booking>> GetBookingByNumber(string bookingNumber)
    {
        var booking = await _bookingService.GetBookingByNumberAsync(bookingNumber);
        if (booking == null)
            return NotFound();

        var userId = GetCurrentUserId();
        if (userId != booking.UserId && !User.IsInRole("Admin") && !User.IsInRole("CSStaff"))
            return Forbid();

        return Ok(booking);
    }


    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<Booking>>> GetUserBookings(int userId)
    {
        // ITC_10.3 / ITC_10.4: userId ≤ 0 is invalid
        if (userId <= 0)
            return BadRequest(new { message = "UserId must be a positive integer." });

        var currentUserId = GetCurrentUserId();
        var hasPrivilegedRole = User.IsInRole("Admin") || User.IsInRole("CSStaff");

        // ITC_10.5: regular user trying to read another user's bookings → 403
        if (currentUserId.HasValue && currentUserId != userId && !hasPrivilegedRole)
            return Forbid();

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

    [HttpGet("status/{status:int}")]
    [Authorize(Roles = "Admin,CSStaff")]
    public async Task<ActionResult<IEnumerable<Booking>>> GetBookingsByStatus(int status)
    {
        // ITC_12.3 / ITC_12.4: validate enum range explicitly so message contains "status"
        if (!Enum.IsDefined(typeof(BookingStatus), status))
            return BadRequest(new { message = $"Invalid status value '{status}'. Valid values are: {string.Join(", ", Enum.GetValues<BookingStatus>().Select(v => $"{(int)v}={v}"))}" });

        var bookings = await _bookingService.GetBookingsByStatusAsync((BookingStatus)status);
        return Ok(bookings);
    }


    [HttpGet("active/user/{userId}")]
    public async Task<ActionResult<Booking>> GetActiveUserBooking(int userId)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId != userId && !User.IsInRole("Admin") && !User.IsInRole("CSStaff"))
            return Forbid();

        var booking = await _bookingService.GetActiveBookingByUserIdAsync(userId);
        if (booking == null)
            return NotFound();

        return Ok(booking);
    }

 
    [HttpGet("active/charging-point/{chargingPointId}")]
    [Authorize(Roles = "Admin,CSStaff")]
    public async Task<ActionResult<Booking>> GetActiveChargingPointBooking(int chargingPointId)
    {
        var booking = await _bookingService.GetActiveBookingByChargingPointIdAsync(chargingPointId);
        if (booking == null)
            return NotFound();

        return Ok(booking);
    }

  
    [HttpPost]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequestDto request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(new { message = "Invalid user token" });

            var bookingRequest = new CreateBookingRequest
            {
                UserId    = userId.Value,
                StationId = request.StationId,
                ChargingPointId = request.ChargingPointId,
                StartTime = request.StartTime,
                EndTime   = request.EndTime,
                BypassActiveUserCheck = User.IsInRole("Admin") || User.IsInRole("CSStaff")
            };

            var result = await _bookingService.CreateBookingAsync(bookingRequest);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Conflict when creating booking");
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when creating booking");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating booking");
            return StatusCode(500, new { message = "An internal error occurred." });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,CSStaff")]
    public async Task<ActionResult<Booking>> UpdateBooking(int id, [FromBody] UpdateBookingRequest request)
    {
        try
        {
            _logger.LogInformation("Updating booking {Id}: Status={Status}", id, request.Status);

     
            var booking = await _bookingService.UpdateBookingAsync(id, request);
            return Ok(booking);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating booking {Id}", id);
            return StatusCode(500, new { message = "An internal error occurred." });
        }
    }

  
    [HttpPost("{id}/cancel")]
    public async Task<ActionResult> CancelBooking(int id)
    {
        var booking = await _bookingService.GetBookingByIdAsync(id);
        if (booking == null)
            return NotFound();

        var currentUserId  = GetCurrentUserId();
        var hasPrivilegedRole = User.IsInRole("Admin") || User.IsInRole("CSStaff");

        if (!currentUserId.HasValue && !hasPrivilegedRole)
        {
            _logger.LogWarning("CancelBooking denied: userId claim missing. BookingId={BookingId}", id);
            return Unauthorized(new { message = "Invalid token: user id claim is missing." });
        }

        if (currentUserId != booking.UserId && !hasPrivilegedRole)
        {
            _logger.LogWarning(
                "CancelBooking forbidden. BookingId={BookingId} OwnerId={OwnerId} CallerId={CallerId}",
                id, booking.UserId, currentUserId);
            return Forbid();
        }

        try
        {
            var result = await _bookingService.CancelBookingAsync(id);
            if (!result)
                return NotFound();

            return Ok();
        }
        catch (InvalidOperationException ex)
        {
       
            return Conflict(new { message = ex.Message });
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
                return NotFound();

            var booking = await _bookingService.GetBookingByIdAsync(id);
            if (booking == null)
                return NotFound();

            return Ok(booking);
        }
        catch (InvalidOperationException ex)
        {
            // ITC_8.3: non-Confirmed → 409
            return Conflict(new { message = ex.Message });
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
                return NotFound();

            var booking = await _bookingService.GetBookingByIdAsync(id);
            if (booking == null)
                return NotFound();

            return Ok(booking);
        }
        catch (InvalidOperationException ex)
        {
            // ITC_9.3: non-InProgress → 409
            return Conflict(new { message = ex.Message });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helper: resolve current user's int ID from multiple JWT claim types
    // ─────────────────────────────────────────────────────────────────────────
    private int? GetCurrentUserId()
    {
        var candidates = new[]
        {
            ClaimTypes.NameIdentifier,
            "nameid",
            "sub",
            "userId",
            "UserId",
            "userid",
            "uid"
        };

        foreach (var claimType in candidates)
        {
            var claim = User.FindFirst(claimType);
            if (claim != null && int.TryParse(claim.Value, out var id))
                return id;
        }

        return null;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// DTO from frontend → service (keeps controller decoupled from service model)
// ─────────────────────────────────────────────────────────────────────────────
public class CreateBookingRequestDto
{
    public int      StationId        { get; set; }
    public int?     ChargingPointId  { get; set; }
    public DateTime StartTime        { get; set; }
    public DateTime? EndTime         { get; set; }
}