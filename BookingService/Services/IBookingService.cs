using EVChargingStation.Shared.Models;

namespace EVChargingStation.BookingService.Services;

public interface IBookingService
{
    Task<Booking?> GetBookingByIdAsync(int id);
    Task<Booking?> GetBookingByNumberAsync(string bookingNumber);
    Task<IEnumerable<Booking>> GetBookingsByUserIdAsync(int userId);
    Task<IEnumerable<Booking>> GetBookingsByChargingPointIdAsync(int chargingPointId);
    Task<IEnumerable<Booking>> GetBookingsByStatusAsync(BookingStatus status);
    Task<Booking> CreateBookingAsync(CreateBookingRequest request);
    Task<Booking> UpdateBookingAsync(int id, UpdateBookingRequest request);
    Task<bool> CancelBookingAsync(int id);
    Task<bool> StartChargingAsync(int id);
    Task<bool> StopChargingAsync(int id);
    Task<Booking?> GetActiveBookingByUserIdAsync(int userId);
    Task<Booking?> GetActiveBookingByChargingPointIdAsync(int chargingPointId);
}

public class CreateBookingRequest
{
    public int UserId { get; set; }
    public int ChargingPointId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
}

public class UpdateBookingRequest
{
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public decimal? EnergyConsumed { get; set; }
    public decimal? TotalCost { get; set; }
    public BookingStatus? Status { get; set; }
}






