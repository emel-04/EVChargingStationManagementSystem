using System.ComponentModel.DataAnnotations;

namespace StationService.DTOs
{
    public class StationCreateDto
    {
        [Required, MaxLength(150)]
        public string Name { get; set; } = null!;
        [Required, MaxLength(250)]
        public string Location { get; set; } = null!;
        public int Power { get; set; }
        public string? Status { get; set; }
    }
}
