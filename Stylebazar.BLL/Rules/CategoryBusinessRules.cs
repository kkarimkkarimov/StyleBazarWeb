using Stylebazar.BLL.Constants;
using Stylebazar.BLL.Exceptions;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;

namespace Stylebazar.BLL.Rules;

public class CategoryBusinessRules
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IProductRepository _productRepository;

    public CategoryBusinessRules(
        ICategoryRepository categoryRepository,
        IProductRepository productRepository)
    {
        _categoryRepository = categoryRepository;
        _productRepository = productRepository;
    }

    public async Task SlugMustBeUniqueAsync(string slug, int? excludeId = null, CancellationToken ct = default)
    {
        bool exists = await _categoryRepository.AnyAsync(
            c => c.Slug == slug && (!excludeId.HasValue || c.Id != excludeId.Value),
            enableTracking: false,
            cancellationToken: ct);

        if (exists)
            throw new BusinessException(Messages.Category.SlugAlreadyExists);
    }

    public async Task<Category> CategoryMustExistAsync(int id, CancellationToken ct = default)
    {
        Category? category = await _categoryRepository.GetAsync(
            c => c.Id == id,
            enableTracking: false,
            cancellationToken: ct);

        if (category is null)
            throw new NotFoundException(nameof(Category), id);

        return category;
    }

    public async Task ParentMustExistIfSpecifiedAsync(int? parentId, CancellationToken ct = default)
    {
        if (!parentId.HasValue)
            return;

        bool exists = await _categoryRepository.AnyAsync(
            c => c.Id == parentId.Value,
            enableTracking: false,
            cancellationToken: ct);

        if (!exists)
            throw new BusinessException(Messages.Category.ParentNotFound);
    }

    public void CannotBeOwnParent(int categoryId, int? parentId)
    {
        if (parentId.HasValue && parentId.Value == categoryId)
            throw new BusinessException(Messages.Category.CannotBeOwnParent);
    }

    /// <summary>
    /// Walks the parent chain upward from <paramref name="newParentId"/>.
    /// If <paramref name="categoryId"/> is reached, the assignment would create a cycle.
    /// </summary>
    public async Task NoCircularReferenceAsync(int categoryId, int? newParentId, CancellationToken ct = default)
    {
        if (!newParentId.HasValue)
            return;

        int? current = newParentId;
        int safetyHops = 0;

        while (current.HasValue && safetyHops++ < 100)
        {
            if (current.Value == categoryId)
                throw new BusinessException(Messages.Category.CircularReference);

            int currentValue = current.Value;
            Category? node = await _categoryRepository.GetAsync(
                c => c.Id == currentValue,
                enableTracking: false,
                cancellationToken: ct);

            current = node?.ParentCategoryId;
        }
    }

    public async Task CannotDeleteWhenHasChildrenAsync(int categoryId, CancellationToken ct = default)
    {
        bool hasChildren = await _categoryRepository.AnyAsync(
            c => c.ParentCategoryId == categoryId,
            enableTracking: false,
            cancellationToken: ct);

        if (hasChildren)
            throw new BusinessException(Messages.Category.HasChildren);
    }

    public async Task CannotDeleteWhenHasProductsAsync(int categoryId, CancellationToken ct = default)
    {
        bool hasProducts = await _productRepository.AnyAsync(
            p => p.CategoryId == categoryId,
            enableTracking: false,
            cancellationToken: ct);

        if (hasProducts)
            throw new BusinessException(Messages.Category.HasProducts);
    }
}
