namespace StationService.DTOs
{
    public class StationListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Location { get; set; } = null!;
        public int Power { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
