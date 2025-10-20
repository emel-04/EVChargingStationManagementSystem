namespace EVCharging.BookingService.DTOs;

public class BookingListResponse
{
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public List<BookingDto> Bookings { get; set; } = new();
}
