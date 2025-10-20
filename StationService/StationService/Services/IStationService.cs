using StationService.DTOs;

namespace StationService.Services
{
    public interface IStationService
    {
        Task<(int total, List<StationListDto> items)> GetPagedAsync(string? q, string? location, int page, int pageSize, int? minPower, int? maxPower, string? status);
        Task<StationListDto?> GetByIdAsync(int id);
        Task<StationListDto> CreateAsync(StationCreateDto dto);
        Task<bool> UpdateAsync(int id, StationUpdateDto dto);
        Task<bool> DeleteAsync(int id);
        Task<bool> UpdateStatusAsync(int id, string status);
    }
}
