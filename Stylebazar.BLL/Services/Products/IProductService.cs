using Core.Persistence.Paging;
using Microsoft.AspNetCore.Http;
using Stylebazar.BLL.DTOs.Products;

namespace Stylebazar.BLL.Services.Products;

public interface IProductService
{
    Task<Paginate<ProductListDto>> GetListAsync(ProductListQueryDto query, CancellationToken ct = default);
    Task<List<ProductListDto>> GetFeaturedAsync(int take = 12, CancellationToken ct = default);
    Task<List<ProductListDto>> GetByStoreAsync(int storeId, CancellationToken ct = default);
    Task<ProductDetailDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ProductDetailDto> GetBySlugAsync(string slug, CancellationToken ct = default);

    Task<ProductDetailDto> CreateAsync(CreateProductDto dto, CancellationToken ct = default);
    Task<ProductDetailDto> UpdateAsync(int id, UpdateProductDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);

    Task<ProductImageDto> AddImageAsync(int productId, IFormFile file, bool isMain, CancellationToken ct = default);
    Task DeleteImageAsync(int productId, int imageId, CancellationToken ct = default);
    Task<ProductImageDto> SetMainImageAsync(int productId, int imageId, CancellationToken ct = default);

    Task<ProductDetailDto> SetFeaturedAsync(int productId, int? days, CancellationToken ct = default);
}
