using Core.Persistence.Paging;
using Microsoft.AspNetCore.Http;
using Stylebazar.BLL.DTOs.Stores;

namespace Stylebazar.BLL.Services.Stores;

public interface IStoreService
{
    Task<Paginate<StoreListDto>> GetListAsync(StoreListQueryDto query, CancellationToken ct = default);
    Task<List<StoreListDto>> GetMyStoresAsync(CancellationToken ct = default);
    Task<StoreDetailDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<StoreDetailDto> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<StoreDetailDto> CreateAsync(CreateStoreDto dto, CancellationToken ct = default);
    Task<StoreDetailDto> UpdateAsync(int id, UpdateStoreDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<StoreDetailDto> ToggleActiveAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Save a new profile (logo / avatar) image for the store. Previous file
    /// on disk is best-effort deleted. Returns the new public URL.
    /// </summary>
    Task<string> UploadProfileImageAsync(int storeId, IFormFile file, CancellationToken ct = default);

    /// <summary>Save a new cover banner image. Previous file best-effort deleted.</summary>
    Task<string> UploadCoverImageAsync(int storeId, IFormFile file, CancellationToken ct = default);

    /// <summary>Removes the cover image (sets CoverImageUrl = null + deletes file).</summary>
    Task RemoveCoverImageAsync(int storeId, CancellationToken ct = default);
}
