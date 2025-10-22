using Microsoft.EntityFrameworkCore;
using StationService.DTOs;
using StationService.Models;
using StationService.Repositories;

namespace StationService.Services
{
    public class StationService : IStationService
    {
        private readonly IStationRepository _repo;
        public StationService(IStationRepository repo) => _repo = repo;

        public async Task<(int total, List<StationListDto> items)> GetPagedAsync(string? q, string? location, int page, int pageSize, int? minPower, int? maxPower, string? status)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 10;

            var query = _repo.Query();

            if (!string.IsNullOrWhiteSpace(q)) query = query.Where(s => s.Name.Contains(q));
            if (!string.IsNullOrWhiteSpace(location)) query = query.Where(s => s.Location.Contains(location));
            if (minPower.HasValue) query = query.Where(s => s.Power >= minPower.Value);
            if (maxPower.HasValue) query = query.Where(s => s.Power <= maxPower.Value);
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(s => s.Status == status);

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(s => s.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(s => new StationListDto {
                    Id = s.Id, Name = s.Name, Location = s.Location, Power = s.Power, Status = s.Status, CreatedAt = s.CreatedAt
                }).ToListAsync();

            return (total, items);
        }

        public async Task<StationListDto?> GetByIdAsync(int id)
        {
            var s = await _repo.GetByIdAsync(id);
            if (s == null) return null;
            return new StationListDto { Id = s.Id, Name = s.Name, Location = s.Location, Power = s.Power, Status = s.Status, CreatedAt = s.CreatedAt };
        }

        public async Task<StationListDto> CreateAsync(StationCreateDto dto)
        {
            var s = new Station { Name = dto.Name, Location = dto.Location, Power = dto.Power, Status = dto.Status ?? "Offline", CreatedAt = DateTime.UtcNow };
            await _repo.AddAsync(s);
            await _repo.SaveChangesAsync();
            return new StationListDto { Id = s.Id, Name = s.Name, Location = s.Location, Power = s.Power, Status = s.Status, CreatedAt = s.CreatedAt };
        }

        public async Task<bool> UpdateAsync(int id, StationUpdateDto dto)
        {
            var s = await _repo.GetByIdAsync(id);
            if (s == null) return false;
            s.Name = dto.Name; s.Location = dto.Location; s.Power = dto.Power; s.Status = dto.Status;
            await _repo.UpdateAsync(s);
            await _repo.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var s = await _repo.GetByIdAsync(id);
            if (s == null) return false;
            await _repo.DeleteAsync(s);
            await _repo.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateStatusAsync(int id, string status)
        {
            var s = await _repo.GetByIdAsync(id);
            if (s == null) return false;
            s.Status = status;
            await _repo.UpdateAsync(s);
            await _repo.SaveChangesAsync();
            return true;
        }
    }
}
