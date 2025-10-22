using Microsoft.EntityFrameworkCore;
using StationService.Data;
using StationService.Models;

namespace StationService.Repositories
{
    public class StationRepository : IStationRepository
    {
        private readonly AppDbContext _db;
        public StationRepository(AppDbContext db) => _db = db;

        public IQueryable<Station> Query() => _db.Stations.AsNoTracking().AsQueryable();

        public async Task<Station?> GetByIdAsync(int id) => await _db.Stations.FindAsync(id);

        public async Task AddAsync(Station s) => await _db.Stations.AddAsync(s);

        public async Task UpdateAsync(Station s)
        {
            _db.Stations.Update(s);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(Station s)
        {
            _db.Stations.Remove(s);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
    }
}
