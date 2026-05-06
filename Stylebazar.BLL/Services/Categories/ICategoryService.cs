using Core.Persistence.Paging;
using Stylebazar.BLL.DTOs.Categories;

namespace Stylebazar.BLL.Services.Categories;

public interface ICategoryService
{
    Task<Paginate<CategoryListDto>> GetListAsync(CategoryListQueryDto query, CancellationToken ct = default);
    Task<CategoryDetailDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<CategoryDetailDto> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<List<CategoryTreeDto>> GetTreeAsync(CancellationToken ct = default);
    Task<CategoryDetailDto> CreateAsync(CreateCategoryDto dto, CancellationToken ct = default);
    Task<CategoryDetailDto> UpdateAsync(int id, UpdateCategoryDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
