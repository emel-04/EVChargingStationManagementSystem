using System;

namespace UserService.Models
{
    public class Vehicle
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string LicensePlate { get; set; }
        public string Model { get; set; }
        public double BatteryCapacity { get; set; }
        public DateTime CreatedAt { get; set; }

        public UserAccount User { get; set; }
    }
}
