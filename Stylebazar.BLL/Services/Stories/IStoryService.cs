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

    /// <summary>
    /// Admin-only: returns every story (active or expired) with store name.
    /// Hard-capped to keep the page snappy.
    /// </summary>
    Task<List<AdminStoryRowDto>> GetAllForAdminAsync(int take = 100, CancellationToken ct = default);
}

public class AdminStoryRowDto
{
    public StoryDto Story { get; set; } = new();
    public string StoreName { get; set; } = string.Empty;
    public string StoreSlug { get; set; } = string.Empty;
}
