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
        public int Status { get; set; } // Legacy: 0-4, Backend may return 1-6
        public DateTime CreatedAt { get; set; }
        public bool CanCreatePayment => Status == 3 || Status == 4;
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