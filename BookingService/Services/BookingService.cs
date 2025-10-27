using EVChargingStation.Shared.Models;
using EVChargingStation.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace EVChargingStation.BookingService.Services;

public class BookingService : IBookingService
{
    private readonly BookingDbContext _context;
    private readonly IQRCodeService _qrCodeService;
    private readonly ILogger<BookingService> _logger;

    public BookingService(BookingDbContext context, IQRCodeService qrCodeService, ILogger<BookingService> logger)
    {
        _context = context;
        _qrCodeService = qrCodeService;
        _logger = logger;
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
        // Check if user has an active booking
        var activeBooking = await GetActiveBookingByUserIdAsync(request.UserId);
        if (activeBooking != null)
        {
            throw new InvalidOperationException("User already has an active booking");
        }

        // Check if charging point is available
        var activeChargingPointBooking = await GetActiveBookingByChargingPointIdAsync(request.ChargingPointId);
        if (activeChargingPointBooking != null)
        {
            throw new InvalidOperationException("Charging point is currently occupied");
        }

        var bookingNumber = GenerateBookingNumber();
        var qrCode = await _qrCodeService.GenerateQRCodeAsync(bookingNumber);

        var booking = new Booking
        {
            UserId = request.UserId,
            ChargingPointId = request.ChargingPointId,
            BookingNumber = bookingNumber,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Status = BookingStatus.Pending,
            QRCode = qrCode,
            CreatedAt = DateTime.UtcNow
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

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

        if (booking.Status == BookingStatus.InProgress)
        {
            throw new InvalidOperationException("Cannot cancel a booking that is in progress");
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

        // Calculate energy consumed and total cost
        if (booking.ActualStartTime.HasValue)
        {
            var duration = booking.ActualEndTime.Value - booking.ActualStartTime.Value;
            // This is a simplified calculation - in reality, you'd get this from the charging station
            booking.EnergyConsumed = (decimal)(duration.TotalHours * 50); // Assuming 50kW average
            booking.TotalCost = booking.EnergyConsumed * 5; // Assuming 5 VND per kWh
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


