using StationService.Models;

namespace StationService.Repositories
{
    public interface IStationRepository
    {
        IQueryable<Station> Query();
        Task<Station?> GetByIdAsync(int id);
        Task AddAsync(Station s);
        Task UpdateAsync(Station s);
        Task DeleteAsync(Station s);
        Task SaveChangesAsync();
    }
}
