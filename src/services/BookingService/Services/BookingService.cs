using EVChargingStation.Shared.Models;
using EVChargingStation.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace EVChargingStation.BookingService.Services;

public class BookingService : IBookingService
{
    private const int LegacyQrCodeColumnSafeLength = 250;
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

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, SemaphoreSlim> _pointLocks = new();
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, SemaphoreSlim> _userLocks = new();
    
    // [BL-FIX-01] Define explicit valid status transitions to prevent invalid state jumps.
    private static readonly IReadOnlyDictionary<BookingStatus, BookingStatus[]> AllowedStatusTransitions =
        new Dictionary<BookingStatus, BookingStatus[]>
        {
            [BookingStatus.Pending] = new[] { BookingStatus.Confirmed, BookingStatus.Cancelled, BookingStatus.Expired },
            [BookingStatus.Confirmed] = new[] { BookingStatus.InProgress, BookingStatus.Cancelled, BookingStatus.Expired },
            [BookingStatus.InProgress] = new[] { BookingStatus.Completed },
            [BookingStatus.Completed] = Array.Empty<BookingStatus>(),
            [BookingStatus.Cancelled] = Array.Empty<BookingStatus>(),
            [BookingStatus.Expired] = Array.Empty<BookingStatus>()
        };

   public async Task<IEnumerable<Booking>> GetAllBookingsAsync()
{
    _logger.LogInformation(" Querying all bookings...");
    
    var bookings = await _context.Bookings
        .OrderByDescending(b => b.CreatedAt)
        .ToListAsync();
    
    _logger.LogInformation($" Returned {bookings.Count} bookings");
    
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
    if (request.StationId <= 0)
    {
        throw new ArgumentException("StationId must be a positive integer.");
    }

    if (request.ChargingPointId.HasValue && request.ChargingPointId.Value <= 0)
    {
        throw new ArgumentException("ChargingPointId must be a positive integer when provided.");
    }

    var startTime = request.StartTime.Kind == DateTimeKind.Utc 
        ? request.StartTime 
        : request.StartTime.ToUniversalTime();

    if (startTime < DateTime.UtcNow.AddMinutes(-5))
    {
        throw new ArgumentException("StartTime cannot be in the past.");
    }

    var endTime = request.EndTime.HasValue
        ? (request.EndTime.Value.Kind == DateTimeKind.Utc ? request.EndTime.Value : request.EndTime.Value.ToUniversalTime())
        : startTime.AddHours(2);

    if (endTime <= startTime)
    {
        throw new ArgumentException("EndTime must be after StartTime.");
    }

    if ((endTime - startTime).TotalHours > 24)
    {
        throw new ArgumentException("Booking duration cannot exceed 24 hours.");
    }

    // TODO: Verify Station and ChargingPoint link via injected IStationValidationService
    // var isValidPoint = await _stationValidationService.ValidateChargingPointAsync(request.StationId, request.ChargingPointId.Value);
    // if (!isValidPoint) throw new ArgumentException("Charging Point does not belong to the Station.");

    if (_userValidationService != null)
    {
        var isUserValid = await _userValidationService.ValidateUserIdAsync(request.UserId);
        if (!isUserValid)
        {
            throw new ArgumentException("Invalid User ID");
        }
    }

    var userLock = _userLocks.GetOrAdd(request.UserId, _ => new SemaphoreSlim(1, 1));
    await userLock.WaitAsync();
    try
    {
        var effectiveChargingPointId = request.ChargingPointId ?? 1;
        var pointLock = _pointLocks.GetOrAdd(effectiveChargingPointId, _ => new SemaphoreSlim(1, 1));
        await pointLock.WaitAsync();
        try
        {
            // [BR-05] ITC_4.6: Mỗi user chỉ được có 1 active booking tại một thời điểm.
            // Active = Pending / Confirmed / InProgress và chưa hết EndTime.
            var now = DateTime.UtcNow;
            if (!request.BypassActiveUserCheck)
            {
                var existingUserActive = await _context.Bookings.AnyAsync(b =>
                    b.UserId == request.UserId &&
                    (b.Status == BookingStatus.Pending ||
                     b.Status == BookingStatus.Confirmed ||
                     b.Status == BookingStatus.InProgress) &&
                    (!b.EndTime.HasValue || b.EndTime > now));

                if (existingUserActive)
                {
                    throw new InvalidOperationException(
                        "User already has an active booking. Please complete or cancel the existing booking first.");
                }
            }

            // [BR-06] ITC_4.8: Charging Point không thể bị đặt 2 lần trong cùng khung giờ.
            // Kiểm tra xem CP đã có booking nào đang active VÀ overlap với [startTime, endTime] không.
            var cpConflict = await _context.Bookings.AnyAsync(b =>
                b.StationId == request.StationId &&
                (b.Status == BookingStatus.Pending ||
                 b.Status == BookingStatus.Confirmed ||
                 b.Status == BookingStatus.InProgress) &&
                b.StartTime < endTime &&
                (!b.EndTime.HasValue || b.EndTime > startTime));

            if (cpConflict)
            {
                throw new InvalidOperationException(
                    "Selected timeslot is not available at this station due to an overlapping booking.");
            }

            var bookingNumber = GenerateBookingNumber();

            var qrPayloadObj = new
            {
                BookingNumber = bookingNumber,
                StationId = request.StationId,
                ChargingPointId = effectiveChargingPointId,
                Start = startTime.ToString("O"),
                End = endTime.ToString("O")
            };
            var qrPayload = System.Text.Json.JsonSerializer.Serialize(qrPayloadObj);
            var generatedQrCode = await _qrCodeService.GenerateQRCodeAsync(qrPayload);
            var qrCode = generatedQrCode;

            // Some environments still have a legacy QRCode column size in MySQL (e.g. varchar(255)).
            // Store compact payload instead of base64 image when it exceeds the safe limit to avoid 500 errors.
            if (generatedQrCode.Length > LegacyQrCodeColumnSafeLength)
            {
                qrCode = qrPayload;
                _logger.LogWarning(
                    "Generated QRCode length {QrLength} exceeded safe DB limit. Falling back to compact payload for booking {BookingNumber}.",
                    generatedQrCode.Length,
                    bookingNumber);
            }

            var booking = new Booking
            {
                UserId = request.UserId,
                StationId = request.StationId,
                ChargingPointId = effectiveChargingPointId,
                BookingNumber = bookingNumber,
                StartTime = startTime,
                EndTime = endTime,
                Status = BookingStatus.Pending,
                QRCode = qrCode,
                CreatedAt = DateTime.UtcNow
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Booking created successfully: {BookingNumber}", booking.BookingNumber);

            return booking;
        }
        finally
        {
            pointLock.Release();
        }
    }
    finally
    {
        userLock.Release();
    }
}

    public async Task<Booking> UpdateBookingAsync(int id, UpdateBookingRequest request)
    {
        var booking = await GetBookingByIdAsync(id);
        if (booking == null)
        {
            throw new KeyNotFoundException("Booking not found");
        }

        var newStart = request.StartTime.HasValue
            ? (request.StartTime.Value.Kind == DateTimeKind.Utc ? request.StartTime.Value : request.StartTime.Value.ToUniversalTime())
            : booking.StartTime;

        var newEnd = request.EndTime.HasValue
            ? (request.EndTime.Value.Kind == DateTimeKind.Utc ? request.EndTime.Value : request.EndTime.Value.ToUniversalTime())
            : (booking.EndTime ?? newStart.AddHours(2));

        if (newEnd <= newStart && !request.EndTime.HasValue)
        {
            newEnd = newStart.AddHours(2);
        }

        if (newEnd <= newStart)
        {
            throw new ArgumentException("EndTime must be after StartTime.");
        }

        if ((newEnd - newStart).TotalHours > 24)
        {
            throw new ArgumentException("Booking duration cannot exceed 24 hours.");
        }
        
        // [BL-FIX-02] Block editing booking details once booking is in terminal state.
        if (booking.Status is BookingStatus.Completed or BookingStatus.Cancelled or BookingStatus.Expired)
        {
            var isAttemptingDataChange =
                request.StartTime.HasValue ||
                request.EndTime.HasValue ||
                request.EnergyConsumed.HasValue ||
                request.TotalCost.HasValue;

            if (isAttemptingDataChange)
            {
                throw new InvalidOperationException($"Cannot modify booking data when status is {booking.Status}.");
            }
        }
        
        // [BL-FIX-03] Guard against invalid negative billing values.
        if (request.EnergyConsumed.HasValue && request.EnergyConsumed.Value < 0)
        {
            throw new ArgumentException("EnergyConsumed cannot be negative.");
        }

        if (request.TotalCost.HasValue && request.TotalCost.Value < 0)
        {
            throw new ArgumentException("TotalCost cannot be negative.");
        }

        if (request.Status.HasValue && request.Status.Value != booking.Status)
        {
            // [BL-FIX-04] Enforce finite-state transition matrix for booking lifecycle.
            if (!AllowedStatusTransitions.TryGetValue(booking.Status, out var allowedTargets) ||
                !allowedTargets.Contains(request.Status.Value))
            {
                throw new InvalidOperationException(
                    $"Cannot transition booking status from {booking.Status} to {request.Status.Value}.");
            }
            booking.Status = request.Status.Value;
        }

        booking.StartTime = newStart;
        booking.EndTime = newEnd;

        if (request.EnergyConsumed.HasValue)
            booking.EnergyConsumed = request.EnergyConsumed.Value;

        if (request.TotalCost.HasValue)
            booking.TotalCost = request.TotalCost.Value;

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

        if (booking.Status == BookingStatus.InProgress)
        {
            throw new InvalidOperationException("Cannot cancel in progress booking.");
        }

        if (booking.Status != BookingStatus.Pending && booking.Status != BookingStatus.Confirmed)
        {
            throw new InvalidOperationException("Only pending/confirmed bookings can be cancelled.");
        }

        if (booking.Status == BookingStatus.Confirmed &&
            DateTime.UtcNow > booking.StartTime.AddMinutes(-30))
        {
            throw new InvalidOperationException("Cannot cancel a confirmed booking within 30 minutes of the start time.");
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

        if (booking.Status == BookingStatus.InProgress)
        {
            // Idempotent for rerun scenarios in Postman collection.
            return true;
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

        if (booking.Status == BookingStatus.Completed)
        {
            // Idempotent behavior for repeated StopCharging calls in integration flow reruns.
            return true;
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
            
            // Lấy giá trị tariff thực tế từ StationService trong tương lai. 
            // Giả lập hiện tại: 50 kWh/giờ, giá 5đ
            decimal powerKwhPerHour = 50m;
            decimal pricePerKwh = 5m;
           
            booking.EnergyConsumed = (decimal)duration.TotalHours * powerKwhPerHour; 
            booking.TotalCost = booking.EnergyConsumed * pricePerKwh;               
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
        var now = DateTime.UtcNow;

        return await _context.Bookings
            .FirstOrDefaultAsync(b =>
                b.UserId == userId &&
                (
                    // InProgress is active only while still inside the booking window.
                    // This avoids stale in-progress records blocking new bookings forever.
                    (b.Status == BookingStatus.InProgress &&
                     (!b.EndTime.HasValue || b.EndTime > now)) ||
                    // Pending/Confirmed are active only while still inside their booking window.
                    ((b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed) &&
                     (!b.EndTime.HasValue || b.EndTime > now))
                ));
    }

    public async Task<Booking?> GetActiveBookingByChargingPointIdAsync(int chargingPointId)
    {
        var now = DateTime.UtcNow;

        return await _context.Bookings
            .FirstOrDefaultAsync(b =>
                b.ChargingPointId == chargingPointId &&
                (
                    (b.Status == BookingStatus.InProgress &&
                     (!b.EndTime.HasValue || b.EndTime > now)) ||
                    ((b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed) &&
                     (!b.EndTime.HasValue || b.EndTime > now))
                ));
    }

    private string GenerateBookingNumber()
    {
        return $"BK{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
    }
    
    public async Task<int> ProcessExpiredBookingsAsync()
    {
        var now = DateTime.UtcNow;
        var expiredBookings = await _context.Bookings
            .Where(b => (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed) 
                        && b.EndTime < now)
            .ToListAsync();

        foreach (var booking in expiredBookings)
        {
            booking.Status = BookingStatus.Expired;
            booking.UpdatedAt = now;
        }

        if (expiredBookings.Any())
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Marked {Count} bookings as Expired.", expiredBookings.Count);
        }

        return expiredBookings.Count;
    }

}