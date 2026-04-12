using EVChargingStation.Shared.Models;
using EVChargingStation.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace EVChargingStation.BookingService.Services;

public class BookingService : IBookingService
{
    private readonly BookingDbContext _context;
    private readonly IQRCodeService _qrCodeService;
    private readonly ILogger<BookingService> _logger;
    private readonly IUserValidationService? _userValidationService;

    public BookingService(
        BookingDbContext context, 
        IQRCodeService qrCodeService, 
        ILogger<BookingService> logger,
        IUserValidationService? userValidationService = null)
    {
        _context = context;
        _qrCodeService = qrCodeService;
        _logger = logger;
        _userValidationService = userValidationService;
    }

   public async Task<IEnumerable<Booking>> GetAllBookingsAsync()
{
    _logger.LogInformation("🔍 Querying all bookings...");
    
    var bookings = await _context.Bookings
        .OrderByDescending(b => b.CreatedAt)
        .ToListAsync();
    
    _logger.LogInformation($"📊 Returned {bookings.Count} bookings");
    
    return bookings;
}

    public async Task<Booking?> GetBookingByIdAsync(int id)
    {
        return await _context.Bookings
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<Booking?> GetBookingByNumberAsync(string bookingNumber)
    {
        return await _context.Bookings
            .FirstOrDefaultAsync(b => b.BookingNumber == bookingNumber);
    }

    public async Task<IEnumerable<Booking>> GetBookingsByUserIdAsync(int userId)
    {
        return await _context.Bookings
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Booking>> GetBookingsByChargingPointIdAsync(int chargingPointId)
    {
        return await _context.Bookings
            .Where(b => b.ChargingPointId == chargingPointId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Booking>> GetBookingsByStatusAsync(BookingStatus status)
    {
        return await _context.Bookings
            .Where(b => b.Status == status)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

  public async Task<Booking> CreateBookingAsync(CreateBookingRequest request)
{
    
    var activeBooking = await GetActiveBookingByUserIdAsync(request.UserId);
    if (activeBooking != null)
    {
        throw new InvalidOperationException("User already has an active booking");
    }

    if (request.ChargingPointId.HasValue)
    {
        if (request.ChargingPointId.Value <= 0)
        {
            throw new ArgumentException("ChargingPointId must be a positive integer.");
        }

        var activeChargingPointBooking = await GetActiveBookingByChargingPointIdAsync(request.ChargingPointId.Value);
        if (activeChargingPointBooking != null)
        {
            throw new InvalidOperationException("Charging point is currently occupied");
        }
    }

    var bookingNumber = GenerateBookingNumber();
    var qrCode = await _qrCodeService.GenerateQRCodeAsync(bookingNumber);

    var booking = new Booking
    {
        UserId = request.UserId,
        StationId = request.StationId,
        ChargingPointId = request.ChargingPointId,
        BookingNumber = bookingNumber,
        StartTime = request.StartTime,
        EndTime = request.EndTime ?? request.StartTime.AddHours(2),
        Status = BookingStatus.Pending,
        QRCode = qrCode,
        CreatedAt = DateTime.UtcNow
    };

    _context.Bookings.Add(booking);
    _logger.LogInformation("Has changes before SaveChanges: {HasChanges}", _context.ChangeTracker.HasChanges());

    await _context.SaveChangesAsync();

    _logger.LogInformation("Has changes after SaveChanges: {HasChanges}", _context.ChangeTracker.HasChanges());
    _logger.LogInformation("Booking ID after SaveChanges: {BookingId}", booking.Id);
    _logger.LogInformation("Booking created successfully: {BookingNumber}", booking.BookingNumber);

    return booking;
}


    public async Task<Booking> UpdateBookingAsync(int id, UpdateBookingRequest request)
    {
        var booking = await GetBookingByIdAsync(id);
        if (booking == null)
        {
            throw new ArgumentException("Booking not found");
        }

        if (request.StartTime.HasValue)
            booking.StartTime = request.StartTime.Value;

        if (request.EndTime.HasValue)
            booking.EndTime = request.EndTime.Value;

        if (request.EnergyConsumed.HasValue)
            booking.EnergyConsumed = request.EnergyConsumed.Value;

        if (request.TotalCost.HasValue)
            booking.TotalCost = request.TotalCost.Value;

        if (request.Status.HasValue)
            booking.Status = request.Status.Value;

        booking.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Booking updated successfully: {BookingNumber}", booking.BookingNumber);

        return booking;
    }

    public async Task<bool> CancelBookingAsync(int id)
    {
        var booking = await GetBookingByIdAsync(id);
        if (booking == null)
        {
            return false;
        }

//         B-03 FIX] Chặn việc hủy các booking đã ở trạng thái kết thúc (terminal state).
// Các booking đã ở trạng thái Cancelled hoặc Completed thì không được phép thay đổi
        if (booking.Status == BookingStatus.Cancelled)
        {
            throw new InvalidOperationException("Booking is already cancelled.");
        }

        if (booking.Status == BookingStatus.Completed)
        {
            throw new InvalidOperationException("Cannot cancel a booking that has already been completed.");
        }

        if (booking.Status == BookingStatus.InProgress)
        {
            throw new InvalidOperationException("Cannot cancel a booking that is in progress.");
        }

        booking.Status = BookingStatus.Cancelled;
        booking.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Booking cancelled: {BookingNumber}", booking.BookingNumber);

        return true;
    }
    public async Task<bool> StartChargingAsync(int id)
    {
        var booking = await GetBookingByIdAsync(id);
        if (booking == null)
        {
            return false;
        }

        if (booking.Status != BookingStatus.Confirmed)
        {
            throw new InvalidOperationException("Booking must be confirmed before starting charging");
        }

        booking.Status = BookingStatus.InProgress;
        booking.ActualStartTime = DateTime.UtcNow;
        booking.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Charging started: {BookingNumber}", booking.BookingNumber);

        return true;
    }

    public async Task<bool> StopChargingAsync(int id)
    {
        var booking = await GetBookingByIdAsync(id);
        if (booking == null)
        {
            return false;
        }

        if (booking.Status != BookingStatus.InProgress)
        {
            throw new InvalidOperationException("Booking is not in progress");
        }

        booking.Status = BookingStatus.Completed;
        booking.ActualEndTime = DateTime.UtcNow;
        booking.UpdatedAt = DateTime.UtcNow;

        // [B-02 FIX] Chỉ tính phí (billing) khi ActualStartTime đã có giá trị.
// Nếu ActualStartTime là null
// thì các giá trị liên quan sẽ được gán rõ ràng là null để tránh việc:
// bị tính thành 0 một cách “im lặng” (sai logic)
// hoặc bị thiếu dữ liệu billing mà không phát hiện ra
        if (booking.ActualStartTime.HasValue)
        {
            var duration = booking.ActualEndTime!.Value - booking.ActualStartTime.Value;
           
            booking.EnergyConsumed = (decimal)(duration.TotalHours * 50); 
            booking.TotalCost = booking.EnergyConsumed * 5;               
        }
        else
        {
            
            booking.EnergyConsumed = null;
            booking.TotalCost = null;
            _logger.LogWarning(
                "StopCharging: ActualStartTime is null for booking {BookingNumber}. " +
                "EnergyConsumed and TotalCost set to null.",
                booking.BookingNumber);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Charging stopped: {BookingNumber}", booking.BookingNumber);

        return true;
    }

    public async Task<Booking?> GetActiveBookingByUserIdAsync(int userId)
    {
        return await _context.Bookings
            .FirstOrDefaultAsync(b => b.UserId == userId && 
                                (b.Status == BookingStatus.Pending || 
                                 b.Status == BookingStatus.Confirmed || 
                                 b.Status == BookingStatus.InProgress));
    }

    public async Task<Booking?> GetActiveBookingByChargingPointIdAsync(int chargingPointId)
    {
        return await _context.Bookings
            .FirstOrDefaultAsync(b => b.ChargingPointId == chargingPointId && 
                                (b.Status == BookingStatus.Pending || 
                                 b.Status == BookingStatus.Confirmed || 
                                 b.Status == BookingStatus.InProgress));
    }

    private string GenerateBookingNumber()
    {
        return $"BK{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
    }
    


}


