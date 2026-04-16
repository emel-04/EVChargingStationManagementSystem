namespace EVChargingStation.Shared.Models
{
    // Bản nháp Ver 1: Quên viết Constructor rỗng dẫn đến lỗi 500 khi nhận JSON
    public class UpdateStationRequest_Ver1
    {
        public string? Name { get; set; }
        public double PricePerKwh { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}