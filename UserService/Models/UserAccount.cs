using System;

namespace UserService.Models
{
    /// <summary>
    /// Thông tin tài khoản người dùng
    /// </summary>
    public class UserAccount
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string FullName { get; set; }
        public string Password { get; set; }
        public string Role { get; set; } // EVDriver, Staff, Admin
        public DateTime CreatedAt { get; set; }

        public ICollection<Vehicle> Vehicles { get; set; }
        public ICollection<Subscription> Subscriptions { get; set; }
        public ICollection<Transaction> Transactions { get; set; }
    }
}
