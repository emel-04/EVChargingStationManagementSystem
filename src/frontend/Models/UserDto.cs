namespace EVChargingStation.Web.Models
{
    public class UserDto
    {
        public int Id { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public int Role { get; set; }  // EVDriver, CSStaff, Admin
        public bool IsActive { get; set; } = true;
        public string? Password { get; set; }
    }
}
