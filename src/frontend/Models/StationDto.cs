namespace EVChargingStation.Web.Models
{
    public class StationDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int NumberOfPorts { get; set; }
        public double PowerKW { get; set; }
        // Các thuộc tính API trả về nhưng bạn chưa khai báo
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public int Status { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Nếu API trả về mảng charging points
        public List<object>? ChargingPoints { get; set; } 
    }
}
