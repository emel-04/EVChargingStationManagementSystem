using System;

namespace UserService.Models
{
    public class Transaction
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string PaymentId { get; set; }
        public double Amount { get; set; }
        public DateTime CreatedAt { get; set; }

        public UserAccount User { get; set; }
    }
}
