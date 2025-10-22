using Microsoft.EntityFrameworkCore;
using StationService.Models;

namespace StationService.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Station> Stations { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Station>().HasData(
                new Station { Id = 1, Name = "Station A", Location = "District 1", Power = 50, Status = "Online", CreatedAt = DateTime.UtcNow },
                new Station { Id = 2, Name = "Station B", Location = "District 5", Power = 120, Status = "Offline", CreatedAt = DateTime.UtcNow }
            );
        }
    }
}
