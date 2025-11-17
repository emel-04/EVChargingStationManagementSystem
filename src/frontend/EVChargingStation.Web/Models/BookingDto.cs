namespace EVChargingStation.Web.Models
{
    public class BookingDto
    {
        public int Id { get; set; }
        public string BookingNumber { get; set; } = "";
        public int UserId { get; set; }
        public string? UserName { get; set; } = "";
        public int StationId { get; set; }
        public string? StationName { get; set; } = "";
        public int? ChargingPointId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int Status { get; set; } // 0=Pending, 1=Confirmed, 2=InProgress, 3=Completed, 4=Cancelled
        public DateTime CreatedAt { get; set; }
        // Thêm vào BookingDto
public bool CanCreatePayment => Status == 2; // Status 2 = Completed
public bool HasPayment { get; set; }
public decimal? PaymentAmount { get; set; }

    public decimal? TotalAmount { get; set; }
    }

    public class ChargingPointDto
    {
        public int Id { get; set; }
        public int StationId { get; set; }
        public string Status { get; set; } = "";
    }
}