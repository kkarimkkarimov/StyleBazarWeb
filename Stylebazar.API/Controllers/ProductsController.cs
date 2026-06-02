using Core.Persistence.Paging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.DTOs.Products;
using Stylebazar.BLL.Services.Products;

namespace Stylebazar.API.Controllers;

// ProductsController — REST API controller. HTTP sorğusunu qəbul edir, biznes xidmətini çağırır və JSON cavab qaytarır.
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _service;

    public ProductsController(IProductService service)
    {
        _service = service;
    }

    /// <summary>List products with paging, filters and sort. Anonymous callers see only active products by default.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Paginate<ProductListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Paginate<ProductListDto>>> GetList(
        [FromQuery] ProductListQueryDto query,
        CancellationToken ct)
    {
        // Default public view: only active products. Admin/Seller can override via query.
        if (!User.IsInRole("Admin") && !User.IsInRole("Seller") && !query.IsActive.HasValue)
            query.IsActive = true;

        Paginate<ProductListDto> result = await _service.GetListAsync(query, ct);
        return Ok(result);
    }

    /// <summary>Top featured products for the homepage.</summary>
    [HttpGet("featured")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<ProductListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ProductListDto>>> GetFeatured([FromQuery] int take = 12, CancellationToken ct = default)
    {
        List<ProductListDto> items = await _service.GetFeaturedAsync(take, ct);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDetailDto>> GetById(int id, CancellationToken ct)
    {
        ProductDetailDto dto = await _service.GetByIdAsync(id, ct);
        return Ok(dto);
    }

    [HttpGet("by-slug/{slug}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDetailDto>> GetBySlug(string slug, CancellationToken ct)
    {
        ProductDetailDto dto = await _service.GetBySlugAsync(slug, ct);
        return Ok(dto);
    }

    /// <summary>Create a new product. Caller must be the owner of the StoreId in the body (or Admin).</summary>
    [HttpPost]
    [Authorize(Roles = "Seller,Admin")]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ProductDetailDto>> Create([FromBody] CreateProductDto dto, CancellationToken ct)
    {
        ProductDetailDto created = await _service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Seller,Admin")]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ProductDetailDto>> Update(int id, [FromBody] UpdateProductDto dto, CancellationToken ct)
    {
        ProductDetailDto updated = await _service.UpdateAsync(id, dto, ct);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Seller,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }

    // ---------- Images ----------

    /// <summary>Upload an image for a product (multipart/form-data).</summary>
    [HttpPost("{id:int}/images")]
    [Authorize(Roles = "Seller,Admin")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [ProducesResponseType(typeof(ProductImageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ProductImageDto>> AddImage(
        int id,
        IFormFile file,
        [FromQuery] bool isMain = false,
        CancellationToken ct = default)
    {
        ProductImageDto created = await _service.AddImageAsync(id, file, isMain, ct);
        return CreatedAtAction(nameof(GetById), new { id }, created);
    }

    [HttpDelete("{id:int}/images/{imageId:int}")]
    [Authorize(Roles = "Seller,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteImage(int id, int imageId, CancellationToken ct)
    {
        await _service.DeleteImageAsync(id, imageId, ct);
        return NoContent();
    }

    [HttpPatch("{id:int}/images/{imageId:int}/main")]
    [Authorize(Roles = "Seller,Admin")]
    [ProducesResponseType(typeof(ProductImageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductImageDto>> SetMainImage(int id, int imageId, CancellationToken ct)
    {
        ProductImageDto updated = await _service.SetMainImageAsync(id, imageId, ct);
        return Ok(updated);
    }

    // ---------- Featured ----------

    /// <summary>Mark a product as featured for an optional number of days.</summary>
    [HttpPost("{id:int}/feature")]
    [Authorize(Roles = "Seller,Admin")]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDetailDto>> SetFeatured(int id, [FromBody] SetFeaturedDto dto, CancellationToken ct)
    {
        ProductDetailDto updated = await _service.SetFeaturedAsync(id, dto.Days, ct);
        return Ok(updated);
    }
}
