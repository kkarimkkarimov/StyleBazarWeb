using Core.Persistence.Paging;
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
}
