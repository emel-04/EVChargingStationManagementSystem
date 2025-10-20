using System.ComponentModel.DataAnnotations;

namespace StationService.Models
{
    public class Station
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = null!;

        [Required, MaxLength(250)]
        public string Location { get; set; } = null!;

        public int Power { get; set; } 

        [MaxLength(50)]
        public string? Status { get; set; } 

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
