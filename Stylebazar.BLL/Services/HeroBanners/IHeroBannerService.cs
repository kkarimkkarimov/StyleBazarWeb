using Stylebazar.BLL.DTOs.HeroBanners;

namespace Stylebazar.BLL.Services.HeroBanners;

// IHeroBannerService — bu xidmətin MÜQAVİLƏSİ (interfeys). Hansı əməliyyatların mövcud olacağını təyin edir; gerçəkləşmə ayrı sinifdədir.
public interface IHeroBannerService
{
    Task<List<HeroBannerDto>> GetActiveAsync(CancellationToken ct = default);
    Task<List<HeroBannerDto>> GetAllAsync(CancellationToken ct = default);
    Task<HeroBannerDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<HeroBannerDto> CreateAsync(CreateHeroBannerDto dto, CancellationToken ct = default);
    Task<HeroBannerDto> UpdateAsync(int id, UpdateHeroBannerDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Flips IsActive on a banner. Quick-toggle for the admin list view so
    /// the admin doesn't need to open the edit form for a one-click activate.
    /// </summary>
    Task<HeroBannerDto> ToggleActiveAsync(int id, CancellationToken ct = default);
}
