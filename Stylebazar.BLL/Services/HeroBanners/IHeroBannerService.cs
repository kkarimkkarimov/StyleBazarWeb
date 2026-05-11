using Stylebazar.BLL.DTOs.HeroBanners;

namespace Stylebazar.BLL.Services.HeroBanners;

public interface IHeroBannerService
{
    Task<List<HeroBannerDto>> GetActiveAsync(CancellationToken ct = default);
    Task<List<HeroBannerDto>> GetAllAsync(CancellationToken ct = default);
    Task<HeroBannerDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<HeroBannerDto> CreateAsync(CreateHeroBannerDto dto, CancellationToken ct = default);
    Task<HeroBannerDto> UpdateAsync(int id, UpdateHeroBannerDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
