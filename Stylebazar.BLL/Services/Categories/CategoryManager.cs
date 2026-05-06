using AutoMapper;
using Core.Persistence.Paging;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Stylebazar.BLL.DTOs.Categories;
using Stylebazar.BLL.Helpers;
using Stylebazar.BLL.Rules;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;
using ValidationException = Stylebazar.BLL.Exceptions.ValidationException;

namespace Stylebazar.BLL.Services.Categories;

public class CategoryManager : ICategoryService
{
    private readonly ICategoryRepository _repo;
    private readonly IMapper _mapper;
    private readonly CategoryBusinessRules _rules;
    private readonly IValidator<CreateCategoryDto> _createValidator;
    private readonly IValidator<UpdateCategoryDto> _updateValidator;

    public CategoryManager(
        ICategoryRepository repo,
        IMapper mapper,
        CategoryBusinessRules rules,
        IValidator<CreateCategoryDto> createValidator,
        IValidator<UpdateCategoryDto> updateValidator)
    {
        _repo = repo;
        _mapper = mapper;
        _rules = rules;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<Paginate<CategoryListDto>> GetListAsync(CategoryListQueryDto query, CancellationToken ct = default)
    {
        Paginate<Category> page = await _repo.GetListAsync(
            predicate: c =>
                (!query.ParentCategoryId.HasValue || c.ParentCategoryId == query.ParentCategoryId.Value) &&
                (string.IsNullOrEmpty(query.Search) || c.Name.Contains(query.Search)),
            orderBy: q => q.OrderBy(c => c.Name),
            include: q => q.Include(c => c.ParentCategory!),
            index: query.Page - 1,
            size: query.PageSize,
            enableTracking: false,
            cancellationToken: ct);

        return new Paginate<CategoryListDto>
        {
            Index = page.Index,
            Size = page.Size,
            Count = page.Count,
            Pages = page.Pages,
            Items = _mapper.Map<List<CategoryListDto>>(page.Items)
        };
    }

    public async Task<CategoryDetailDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        Category category = await GetWithChildrenAsync(c => c.Id == id, ct);
        return _mapper.Map<CategoryDetailDto>(category);
    }

    public async Task<CategoryDetailDto> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        Category category = await GetWithChildrenAsync(c => c.Slug == slug, ct);
        return _mapper.Map<CategoryDetailDto>(category);
    }

    public async Task<List<CategoryTreeDto>> GetTreeAsync(CancellationToken ct = default)
    {
        Paginate<Category> all = await _repo.GetListAsync(
            orderBy: q => q.OrderBy(c => c.Name),
            index: 0,
            size: int.MaxValue,
            enableTracking: false,
            cancellationToken: ct);

        Dictionary<int, CategoryTreeDto> map = all.Items
            .ToDictionary(c => c.Id, c => new CategoryTreeDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug
            });

        List<CategoryTreeDto> roots = new();
        foreach (Category c in all.Items)
        {
            CategoryTreeDto node = map[c.Id];
            if (c.ParentCategoryId.HasValue && map.TryGetValue(c.ParentCategoryId.Value, out CategoryTreeDto? parent))
                parent.Children.Add(node);
            else
                roots.Add(node);
        }

        return roots;
    }

    public async Task<CategoryDetailDto> CreateAsync(CreateCategoryDto dto, CancellationToken ct = default)
    {
        ValidationResult validation = await _createValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        string slug = string.IsNullOrWhiteSpace(dto.Slug) ? SlugHelper.Slugify(dto.Name) : dto.Slug.Trim();

        await _rules.ParentMustExistIfSpecifiedAsync(dto.ParentCategoryId, ct);
        await _rules.SlugMustBeUniqueAsync(slug, excludeId: null, ct: ct);

        Category entity = new()
        {
            Name = dto.Name.Trim(),
            Slug = slug,
            ParentCategoryId = dto.ParentCategoryId
        };

        Category created = await _repo.AddAsync(entity, ct);
        return await GetByIdAsync(created.Id, ct);
    }

    public async Task<CategoryDetailDto> UpdateAsync(int id, UpdateCategoryDto dto, CancellationToken ct = default)
    {
        ValidationResult validation = await _updateValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        Category existing = await _rules.CategoryMustExistAsync(id, ct);

        string newSlug = string.IsNullOrWhiteSpace(dto.Slug) ? SlugHelper.Slugify(dto.Name) : dto.Slug.Trim();

        _rules.CannotBeOwnParent(id, dto.ParentCategoryId);
        await _rules.ParentMustExistIfSpecifiedAsync(dto.ParentCategoryId, ct);
        await _rules.NoCircularReferenceAsync(id, dto.ParentCategoryId, ct);
        await _rules.SlugMustBeUniqueAsync(newSlug, excludeId: id, ct: ct);

        // EF tracked entity needed for proper update; reload tracked.
        Category? tracked = await _repo.GetAsync(c => c.Id == id, cancellationToken: ct);
        if (tracked is null)
            throw new Exceptions.NotFoundException(nameof(Category), id);

        tracked.Name = dto.Name.Trim();
        tracked.Slug = newSlug;
        tracked.ParentCategoryId = dto.ParentCategoryId;

        await _repo.UpdateAsync(tracked, ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        Category existing = await _rules.CategoryMustExistAsync(id, ct);

        await _rules.CannotDeleteWhenHasChildrenAsync(id, ct);
        await _rules.CannotDeleteWhenHasProductsAsync(id, ct);

        Category? tracked = await _repo.GetAsync(c => c.Id == id, cancellationToken: ct);
        if (tracked is null)
            return;

        await _repo.DeleteAsync(tracked, permanent: false, cancellationToken: ct);
    }

    private async Task<Category> GetWithChildrenAsync(System.Linq.Expressions.Expression<Func<Category, bool>> predicate, CancellationToken ct)
    {
        Category? category = await _repo.GetAsync(
            predicate,
            include: q => q
                .Include(c => c.ParentCategory!)
                .Include(c => c.ChildCategories),
            enableTracking: false,
            cancellationToken: ct);

        if (category is null)
            throw new Exceptions.NotFoundException(nameof(Category), 0);

        return category;
    }
}
