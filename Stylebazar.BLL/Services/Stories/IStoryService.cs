using Microsoft.AspNetCore.Http;
using Stylebazar.BLL.DTOs.Stories;

namespace Stylebazar.BLL.Services.Stories;

public interface IStoryService
{
    Task<List<StoreStoriesDto>> GetActiveFeedAsync(CancellationToken ct = default);
    Task<List<StoryDto>> GetByStoreAsync(int storeId, bool activeOnly = true, CancellationToken ct = default);
    Task<StoryDto> CreateAsync(int storeId, CreateStoryDto dto, IFormFile file, CancellationToken ct = default);
    Task<StoryDto> RecordViewAsync(int id, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
