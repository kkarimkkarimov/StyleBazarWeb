using Core.Persistence.Paging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.DTOs.Categories;
using Stylebazar.BLL.Services.Categories;

namespace Stylebazar.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _service;

    public CategoriesController(ICategoryService service)
    {
        _service = service;
    }

    /// <summary>List categories with pagination, optional parent filter and name search.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Paginate<CategoryListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Paginate<CategoryListDto>>> GetList(
        [FromQuery] CategoryListQueryDto query,
        CancellationToken ct)
    {
        Paginate<CategoryListDto> result = await _service.GetListAsync(query, ct);
        return Ok(result);
    }

    /// <summary>Hierarchical tree of all categories.</summary>
    [HttpGet("tree")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<CategoryTreeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CategoryTreeDto>>> GetTree(CancellationToken ct)
    {
        List<CategoryTreeDto> tree = await _service.GetTreeAsync(ct);
        return Ok(tree);
    }

    /// <summary>Get a category by id, including parent name and direct children.</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CategoryDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDetailDto>> GetById(int id, CancellationToken ct)
    {
        CategoryDetailDto dto = await _service.GetByIdAsync(id, ct);
        return Ok(dto);
    }

    /// <summary>Get a category by slug.</summary>
    [HttpGet("by-slug/{slug}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CategoryDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDetailDto>> GetBySlug(string slug, CancellationToken ct)
    {
        CategoryDetailDto dto = await _service.GetBySlugAsync(slug, ct);
        return Ok(dto);
    }

    /// <summary>Create a new category. Admin only.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CategoryDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CategoryDetailDto>> Create(
        [FromBody] CreateCategoryDto dto,
        CancellationToken ct)
    {
        CategoryDetailDto created = await _service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Update a category. Admin only.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CategoryDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CategoryDetailDto>> Update(
        int id,
        [FromBody] UpdateCategoryDto dto,
        CancellationToken ct)
    {
        CategoryDetailDto updated = await _service.UpdateAsync(id, dto, ct);
        return Ok(updated);
    }

    /// <summary>Soft-delete a category. Admin only.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}
