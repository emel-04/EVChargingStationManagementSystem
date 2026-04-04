namespace EVChargingStation.Web.Models
{
    public class PaymentDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? BookingId { get; set; }
        public string PaymentNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int Method { get; set; } // Legacy: 0-2, Backend may return 1-6
        public int Status { get; set; } // Legacy: 0-3, Backend may return 1-6
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
            1 => "Thẻ tín dụng",
            2 => "Thẻ ghi nợ",
            3 => "Ví điện tử",
            4 => "Chuyển khoản",
            5 => "Tiền mặt",
            6 => "Voucher",
            _ => "Không xác định"
        };
        
        public string StatusName => Status switch
        {
            0 => "Chờ xử lý",
            1 => "Chờ xử lý",
            2 => "Đang xử lý",
            3 => "Hoàn thành",
            4 => "Thất bại",
            5 => "Hoàn tiền",
            6 => "Đã hủy",
            _ => "Không xác định"
        };
        
        public string StatusBadgeClass => Status switch
        {
            0 => "bg-warning",
            1 => "bg-warning",
            2 => "bg-primary",
            3 => "bg-success",
            4 => "bg-danger",
            5 => "bg-info",
            6 => "bg-secondary",
            _ => "bg-dark"
        };
    }
}