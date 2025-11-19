namespace EVChargingStation.Web.Models
{
    public class PaymentDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? BookingId { get; set; }
        public string PaymentNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int Method { get; set; } // 0=Cash, 1=Card, 2=EWallet
        public int Status { get; set; } // 0=Pending, 1=Completed, 2=Failed, 3=Refunded
        public string? Description { get; set; }
        public string? TransactionId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }

         public string? StationName { get; set; }
        
        // Display properties
        public string UserName { get; set; } = string.Empty;
        
        public string MethodName => Method switch
        {
            0 => "Tiền mặt",
            1 => "Thẻ ngân hàng",
            2 => "Ví điện tử",
            _ => "Không xác định"
        };
        
        public string StatusName => Status switch
        {
            0 => "Chờ xử lý",
            1 => "Hoàn thành",
            2 => "Thất bại",
            3 => "Hoàn tiền",
            _ => "Không xác định"
        };
        
        public string StatusBadgeClass => Status switch
        {
            0 => "bg-warning",
            1 => "bg-success",
            2 => "bg-danger",
            3 => "bg-info",
            _ => "bg-dark"
        };
    }
}